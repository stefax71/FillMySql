using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.SharpDevelop.Editor;
using Microsoft.Win32;

namespace FillMySQL
{
    public partial class MainWindow
    {

        private readonly MainWindowModel _mainWindowModel;
        private TextMarkerService _textMarkerService;
        private (ITextMarker _sqlMarker, ITextMarker _paramMarker, ITextMarker _genericTextBefore, ITextMarker _genericTextAfter) _currentMarker;

        public MainWindow()
        {
            InitializeComponent();
            
            OriginalQuery.IsReadOnly = false;
            _mainWindowModel = new MainWindowModel();
            DataContext = _mainWindowModel;
            _mainWindowModel.PropertyChanged += PropertyHasChanged;
            
            OriginalQuery.WordWrap = true;
            OriginalQuery.ShowLineNumbers = true;
            OriginalQuery.IsReadOnly = true;
            OriginalQuery.TextArea.Caret.PositionChanged += CaretPositionChanged;
            
            OriginalQuery.PreviewKeyDown += OriginalQuery_PreviewKeyDown;
            OriginalQuery.PreviewMouseWheel += QueryBoxOnPreviewMouseWheel;
            ProcessedQuery.PreviewMouseWheel += QueryBoxOnPreviewMouseWheel;
            
            PropertyHasChanged(_mainWindowModel, new PropertyChangedEventArgs(nameof(_mainWindowModel.CanBrowseToNextRecord)));
            PropertyHasChanged(_mainWindowModel, new PropertyChangedEventArgs(nameof(_mainWindowModel.CanBrowseToPreviousRecord)));
            
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FillMySQL.Resources.sql.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    IHighlightingDefinition customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    OriginalQuery.SyntaxHighlighting = customHighlighting;
                    ProcessedQuery.SyntaxHighlighting = customHighlighting;
                    var colorizer = new SqlColorizingTransformer(_mainWindowModel.SqlProcessor, customHighlighting);
                    OriginalQuery.TextArea.TextView.LineTransformers.RemoveAt(0);
                    OriginalQuery.TextArea.TextView.LineTransformers.Add(colorizer);
                    InitializeTextMarkerService();
                    
                }
            }
        }

        private void QueryBoxOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textEditor = (TextEditor)sender;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
            textEditor.FontSize = e.Delta switch
            {
                > 0 => Math.Min(textEditor.FontSize + 1, 20),
                < 0 => Math.Max(textEditor.FontSize - 1, 10),
                _ => textEditor.FontSize
            };
            e.Handled = true;
        }

        void InitializeTextMarkerService()
        {
            _textMarkerService = new TextMarkerService(OriginalQuery.Document);
            OriginalQuery.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            OriginalQuery.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            if (OriginalQuery.Document.ServiceProvider.GetService(typeof(IServiceContainer)) is IServiceContainer services)
                services.AddService(typeof(ITextMarkerService), _textMarkerService);
        }        

        private void CaretPositionChanged(object sender, EventArgs e)
        {
            try
            {
                ProcessedQuery.Text = "";
                Caret caret = (Caret)sender;
                var (_, qd) = _mainWindowModel.SqlProcessor.GetQueryAtCharacterPosition(caret.Offset);
                RemoveOldMarkers();
                ProcessQueryData(qd);
            }
            catch (Exception ex)
            {
                // Nothing
            }
        }

        private void LoadFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() != true) return;
                _mainWindowModel.SqlProcessor.LoadFile(openFileDialog.FileName);
                OriginalQuery.Document.Text = _mainWindowModel.SqlProcessor.SqlString;
                NavigateToQueryWithIndex(1);
            }
            catch (ArgumentException ex)
            {
                DisplayError(ex);
            }
        }
        

        private void PropertyHasChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_mainWindowModel.CanBrowseToNextRecord):
                    BrowseNextQuery.IsEnabled = _mainWindowModel.CanBrowseToNextRecord;
                    break;
                
                case nameof(_mainWindowModel.CanBrowseToPreviousRecord):
                    BrowsePreviousQuery.IsEnabled = _mainWindowModel.CanBrowseToPreviousRecord;
                    break;
            }
        }


        private void ProcessQueryData(QueryData? queryData)
        {
            if (queryData == null) return;
            
            QueryData content = queryData.Value;
            ChangeQueryBackgroundInOriginalQueryTextBox(content);
            var processedString = _mainWindowModel.SqlProcessor.ProcessQueryFromQueryData(content);
            _mainWindowModel.CurrentQueryIndex = content.Index;
            ProcessedQuery.Text = processedString;
            
        }

        private void ChangeQueryBackgroundInOriginalQueryTextBox(QueryData content)
        {
            RemoveOldMarkers();
            AddMarkerForQuery(content);
            AddMarkerForParams(content);
            AddMarkerForRestOfText(content);
            ScrollViewToOffset(content.SqlStartPosition);
        }

        private void AddMarkerForRestOfText(QueryData content)
        {
            _currentMarker._genericTextBefore = _textMarkerService.Create(0,
                content.SqlStartPosition);
            _currentMarker._genericTextBefore.ForegroundColor = Colors.Gray;

            _currentMarker._genericTextAfter = _textMarkerService.Create(Math.Max(content.SqlEndPosition, content.ParamsEndPosition),
                OriginalQuery.Document.TextLength - Math.Max(content.SqlEndPosition, content.ParamsEndPosition));
            _currentMarker._genericTextAfter.ForegroundColor = Colors.Gray;
        }

        private void AddMarkerForQuery(QueryData content)
        {
            _currentMarker._sqlMarker = _textMarkerService.Create(content.SqlStartPosition,
                content.Query.Length);
            _currentMarker._sqlMarker.BackgroundColor = Colors.Yellow;
        }

        private void AddMarkerForParams(QueryData content)
        {
            if (content.ParamsStartPosition <= 0 || content.ParamsEndPosition <= 0) return;
            
            _currentMarker._paramMarker = _textMarkerService.Create(content.ParamsStartPosition,
                content.QueryParameters.Length);
            _currentMarker._paramMarker.BackgroundColor = Colors.Chartreuse;
        }

        private void ScrollViewToOffset(in int offset)
        {
            OriginalQuery.TextArea.Caret.Offset = offset;
            OriginalQuery.TextArea.Caret.BringCaretToView();
        }

        private void RemoveOldMarkers()
        {
            if (_currentMarker._sqlMarker != null) _textMarkerService.Remove(_currentMarker._sqlMarker);
            if (_currentMarker._paramMarker != null) _textMarkerService.Remove(_currentMarker._paramMarker);
            if (_currentMarker._genericTextBefore != null) _textMarkerService.Remove(_currentMarker._genericTextBefore);
            if (_currentMarker._genericTextAfter != null) _textMarkerService.Remove(_currentMarker._genericTextAfter);
        }

        private void ProcessPasteOperation()
        {
            try
            {
                if (!ConfirmOverwriteCurrentText()) return;
                var text = Clipboard.GetText();
                _mainWindowModel.SqlProcessor.Load(text);
                OriginalQuery.Document.Text = text;
            }
            catch (ArgumentException ex)
            {
                DisplayError(ex);
            }

        }
        
        private void OriginalQuery_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control || e.Key != Key.V) return;
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(Clipboard.GetText())) return;
            ProcessPasteOperation();
        }

        private void DisplayError(ArgumentException ex)
        {
            if (ex.Message == "String does not contain any query")
            {
                ShowErrorInStatusBar(ex.Message);
            }
        }

        private void GoToNextQuery_OnClick(object sender, RoutedEventArgs e)
        {
            var requestedQueryIndex = _mainWindowModel.CurrentQueryIndex + 1;
            NavigateToQueryWithIndex(requestedQueryIndex);
        }

        private void BrowsePreviousQuery_OnClick(object sender, RoutedEventArgs e)
        {
            var requestedQueryIndex = _mainWindowModel.CurrentQueryIndex - 1;

            NavigateToQueryWithIndex(requestedQueryIndex);
        }

        private void NavigateToQueryWithIndex(int requestedQueryIndex)
        {
            try
            {
                RestoreStatusBar();
                QueryData queryData =
                    _mainWindowModel.SqlProcessor.GetQueryAtPosition(requestedQueryIndex);

                ProcessQueryData(queryData);
            }
            catch (Exception ex)
            {
                ShowErrorInStatusBar(ex.Message);
            }
        }

        private bool ConfirmOverwriteCurrentText()
        {
            var result = MessageBox.Show("Pasting the text from clipboard will overwrite the current one. \r\nAre you sure?",
                    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            return result == MessageBoxResult.Yes;
        }

        private void UnlockBox_OnClick(object sender, RoutedEventArgs e)
        {
            OriginalQuery.IsReadOnly = !OriginalQuery.IsReadOnly;
        }

        private void ShowErrorInStatusBar(string message)
        {
            StatusBar.Background = Brushes.Red;
            StatusBarMessage.Text = message;
        }

        private void RestoreStatusBar()
        {
            StatusBar.Background = Brushes.Gray;
            StatusBarMessage.Text = "";
        }
    }
}