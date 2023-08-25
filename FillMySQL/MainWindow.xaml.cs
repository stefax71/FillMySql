using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace FillMySQL
{
    public partial class MainWindow
    {
        private readonly SqlProcessor _sqlProcessor;
        private TextRange _currentQueryRange = null;
        private TextRange _currentParamRange = null;
        private Brush _originalBackgroundBrush = null;
        private int _currentQueryIndex = -1;

        private ObservableCollection<string> Queries => _sqlProcessor.Queries;

        public MainWindow()
        {
            _sqlProcessor = new SqlProcessor();
            InitializeComponent();
            OriginalQuery.PreviewKeyDown += OriginalQuery_PreviewKeyDown;
            OriginalQuery.IsReadOnly = true;
            OriginalQuery.IsReadOnlyCaretVisible = true;
            OriginalQuery.SelectionChanged += OriginalQueryOnSelectionChanged;
        }

        private void OriginalQueryOnSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentPosition = CalculateCurrentPositionInText();
                (_currentQueryIndex, QueryData? queryData) = _sqlProcessor.GetQueryAtCharacterPosition(currentPosition);
                if (queryData != null)
                {
                    QueryData content = queryData.Value;
                    ChangeQueryBackgroundInOriginalQueryTextBox(content);
                    var processedString = _sqlProcessor.ProcessQueryFromQueryData(content);
                    ProcessedQuery.Text = processedString;
                }
                else
                {
                    Console.WriteLine("Non c'è nulla alla posizione " + currentPosition);
                    ProcessedQuery.Text = "";
                }
            }
            catch (Exception ex)
            {
                // Ignore the exception as it can be thrown when clicking on the empty textbox
            }

        }

        private void ChangeQueryBackgroundInOriginalQueryTextBox(QueryData content)
        {
            if (_currentQueryRange != null)
            {
                _currentQueryRange.ApplyPropertyValue(TextElement.BackgroundProperty, _originalBackgroundBrush);
                _currentParamRange.ApplyPropertyValue(TextElement.BackgroundProperty, _originalBackgroundBrush);
            }

            // Must calculate all of them in advance, cannot extract single method because color would add spaces!
            // We could process each character in the text to check if it's a control character but would slow down
            TextPointer sqlStartPointer = FindTextPointerByPosition(OriginalQuery.Document, content.SqlStartPosition + 1);
            TextPointer sqlEndPointer = FindTextPointerByPosition(OriginalQuery.Document, content.SqlEndPosition + 1);
            TextPointer paramStartPointer = FindTextPointerByPosition(OriginalQuery.Document, content.ParamsStartPosition + 1);
            TextPointer paramEndPointer = FindTextPointerByPosition(OriginalQuery.Document, content.ParamsEndPosition + 1);

            _currentQueryRange = new TextRange(sqlStartPointer, sqlEndPointer);
            _originalBackgroundBrush = _currentQueryRange.GetPropertyValue(TextElement.BackgroundProperty) as Brush;
            _currentQueryRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);

            _currentParamRange = new TextRange(paramStartPointer, paramEndPointer);
            _originalBackgroundBrush = _currentParamRange.GetPropertyValue(TextElement.BackgroundProperty) as Brush;
            _currentParamRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Chartreuse);
        }

        private void ColorizeRange(int from, int to, Brush color)
        {
            TextPointer sqlStartPointer = FindTextPointerByPosition(OriginalQuery.Document, from + 1);
            TextPointer sqlEndPointer = FindTextPointerByPosition(OriginalQuery.Document, to);
            
            _currentQueryRange = new TextRange(sqlStartPointer, sqlEndPointer);
            _originalBackgroundBrush = _currentQueryRange.GetPropertyValue(TextElement.BackgroundProperty) as Brush;
            _currentQueryRange.ApplyPropertyValue(TextElement.BackgroundProperty, color);    
        }
        
        private TextPointer FindTextPointerByPosition(FlowDocument document, int targetPosition)
        {
            int currentPosition = 0;
            foreach (Block block in document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    int paragraphLength = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Length;
                    if (currentPosition + paragraphLength >= targetPosition)
                    {
                        int charIndex = targetPosition - currentPosition;
                        return paragraph.ContentStart.GetPositionAtOffset(charIndex);
                    }
                    currentPosition += (paragraphLength  + 2);
                }
            }
            return null;
        }

        private int CalculateCurrentPositionInText()
        {
            TextRange range = new TextRange(OriginalQuery.Document.ContentStart, OriginalQuery.CaretPosition);
            return range.Text.Length + 1;
        }

        private void LoadFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() != true) return;
                _sqlProcessor.LoadFile(openFileDialog.FileName);
                LoadStringAsQuery(_sqlProcessor.OriginalStringAsArray());
            }
            catch (ArgumentException ex)
            {
                DisplayError(ex);
            }

        }

        private void ProcessPasteOperation()
        {
            try
            {
                if (IsQueryBoxEmpty() || (!IsQueryBoxEmpty() && ConfirmOverwriteCurrentText()))
                {
                    var text = Clipboard.GetText();
                    _sqlProcessor.Load(text);
                    LoadStringAsQuery(new[] {text});
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
            OriginalQuery.Document = CreateDocumentFromSqlString(inputStrings);
            var queryData = _sqlProcessor.GetQueryProcessed(1);
            ProcessedQuery.Text = queryData;
        }

        private void RestoreStatusBar()
        {
            StatusBar.Background = Brushes.Gray;
            StatusBarMessage.Text = "";
        }

        private FlowDocument CreateDocumentFromSqlString(IEnumerable<string> inputLines)
        {
            var document = new FlowDocument();

            foreach (var currentLine in inputLines)
            {
                var par = new Paragraph(new Run(currentLine))
                {
                    Margin = new Thickness(0)
                };
                document.Blocks.Add(par);
            }
            return document;
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
                StatusBar.Background = Brushes.Red;
                StatusBarMessage.Text = ex.Message;
            }
        }

        private bool IsQueryBoxEmpty()
        {
            return string.IsNullOrWhiteSpace(
                new TextRange(OriginalQuery.Document.ContentStart, OriginalQuery.Document.ContentEnd).Text);
        }

        private void GoToNextQuery_OnClick(object sender, RoutedEventArgs e)
        {
            // do nothing
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

    }
}