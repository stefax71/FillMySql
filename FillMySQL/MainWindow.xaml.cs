using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.SharpDevelop.Editor;
using Microsoft.Win32;

namespace FillMySQL
{
    public partial class MainWindow
    {

        private readonly MainWindowModel _mainWindowModel;
        private readonly DocumentColorizingTransformer colorizer;
        private TextMarkerService textMarkerService;
        private (ITextMarker _sqlMarker, ITextMarker _paramMarker) _currentMarker;

        public MainWindow()
        {
            InitializeComponent();
            OriginalQuery.PreviewKeyDown += OriginalQuery_PreviewKeyDown;
            OriginalQuery.IsReadOnly = false;
            _mainWindowModel = new MainWindowModel();
            DataContext = _mainWindowModel;
            _mainWindowModel.PropertyChanged += PropertyHasChanged;
            OriginalQuery.WordWrap = true;
            OriginalQuery.ShowLineNumbers = true;
            
            PropertyHasChanged(_mainWindowModel, new PropertyChangedEventArgs(nameof(_mainWindowModel.CanBrowseToNextRecord)));
            PropertyHasChanged(_mainWindowModel, new PropertyChangedEventArgs(nameof(_mainWindowModel.CanBrowseToPreviousRecord)));
            
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FillMySQL.Resources.sql.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    IHighlightingDefinition customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    OriginalQuery.SyntaxHighlighting = customHighlighting;
                    colorizer = new SqlColorizingTransformer(_mainWindowModel.SqlProcessor, customHighlighting);
                }
            }
            OriginalQuery.TextArea.TextView.LineTransformers.RemoveAt(0);
            OriginalQuery.TextArea.TextView.LineTransformers.Add(colorizer);
            InitializeTextMarkerService();
        }
        
        void InitializeTextMarkerService()
        {
            var textMarkerService = new TextMarkerService(OriginalQuery.Document);
            OriginalQuery.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
            OriginalQuery.TextArea.TextView.LineTransformers.Add(textMarkerService);
            IServiceContainer services = (IServiceContainer)OriginalQuery.Document.ServiceProvider.GetService(typeof(IServiceContainer));
            if (services != null)
                services.AddService(typeof(ITextMarkerService), textMarkerService);
            this.textMarkerService = textMarkerService;
        }        

        private void CaretPositionChanged(object sender, EventArgs e)
        {
            Caret caret = (Caret)sender;
            int caretLine = caret.Line;
            int startOffset = 10; // Esempio
            int endOffset = 50;   // Esempio

            
            Console.WriteLine("Processing for " + caretLine);
            if (caretLine >= startOffset && caretLine <= endOffset)
            {
                if (!OriginalQuery.TextArea.TextView.LineTransformers.Contains(colorizer))
                {
                    OriginalQuery.TextArea.TextView.LineTransformers.Add(colorizer);
                }
            }
            else
            {
                OriginalQuery.TextArea.TextView.LineTransformers.Remove(colorizer);
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
                LoadStringAsQuery(_mainWindowModel.SqlProcessor.OriginalStringAsArray());
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
            _mainWindowModel.CurrentQueryIndex = content.index;
            ProcessedQuery.Text = processedString;
        }

        private void ChangeQueryBackgroundInOriginalQueryTextBox(QueryData content)
        {
            RemoveOldMarkers();
            _currentMarker._sqlMarker = textMarkerService.Create(content.SqlStartPosition,
                content.SqlEndPosition - content.SqlStartPosition);
            _currentMarker._sqlMarker.MarkerTypes = TextMarkerTypes.DottedUnderline;
            _currentMarker._sqlMarker.MarkerColor = Colors.Blue;
            _currentMarker._sqlMarker.BackgroundColor = Colors.Yellow;
            
            _currentMarker._paramMarker = textMarkerService.Create(content.ParamsStartPosition,
                content.ParamsEndPosition - content.ParamsStartPosition);
            _currentMarker._paramMarker.BackgroundColor = Colors.Chartreuse;
            // OriginalQuery.TextArea.Caret.BringCaretToView();
        }

        private void RemoveOldMarkers()
        {
            if (_currentMarker._sqlMarker != null)
            {
                textMarkerService.Remove(_currentMarker._sqlMarker);
            }

            if (_currentMarker._paramMarker != null)
            {
                textMarkerService.Remove(_currentMarker._paramMarker);
            }
        }

        private void ProcessPasteOperation()
        {
            try
            {
                if (ConfirmOverwriteCurrentText())
                {
                    var text = Clipboard.GetText();
                    _mainWindowModel.SqlProcessor.Load(text);
                    OriginalQuery.Document.Text = text;
                    // LoadStringAsQuery(new[] {text});
                }
            }
            catch (ArgumentException ex)
            {
                DisplayError(ex);
            }

        }

        private void LoadStringAsQuery(string[] inputStrings)
        {
            RestoreStatusBar();
            var queryData = _mainWindowModel.SqlProcessor.GetQueryAtPosition(1);
            ProcessQueryData(queryData);
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
            int requestedQueryIndex = _mainWindowModel.CurrentQueryIndex + 1;
            NavigateToQueryWithIndex(requestedQueryIndex);
        }

        private void BrowsePreviousQuery_OnClick(object sender, RoutedEventArgs e)
        {
            int requestedQueryIndex = _mainWindowModel.CurrentQueryIndex - 1;

            NavigateToQueryWithIndex(requestedQueryIndex);
        }

        private void NavigateToQueryWithIndex(int requestedQueryIndex)
        {
            try
            {
                Console.WriteLine("Navigating to query " + requestedQueryIndex);
                QueryData? queryData =
                    _mainWindowModel.SqlProcessor.GetQueryAtPosition(requestedQueryIndex);
                // OriginalQuery.TextArea.Caret.Offset
                //     FindTextPointerByPosition(OriginalQuery.Document, queryData.Value.SqlStartPosition);
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