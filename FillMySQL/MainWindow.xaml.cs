using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Win32;

namespace FillMySQL
{
    public partial class MainWindow
    {
        private readonly SqlProcessor _sqlProcessor;
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
                QueryData? queryData = _sqlProcessor.GetQueryAtCharacterPosition(currentPosition);
                if (queryData != null)
                {
                    QueryData content = queryData.Value;
                    var processedString = _sqlProcessor.ProcessQueryFromQueryData(content);
                    ProcessedQuery.Text = processedString;
                }
                else
                {
                    ProcessedQuery.Text = "";
                }
            }
            catch (Exception ex)
            {
                // Ignore the exception as it can be thrown when clicking on the empty textbox
            }

        }

        private int CalculateCurrentPositionInText()
        {
            TextRange range = new TextRange(OriginalQuery.Document.ContentStart, OriginalQuery.CaretPosition);
            return range.Text.Length + 1;
        }

        private void LoadFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != true) return;
            _sqlProcessor.LoadFile(openFileDialog.FileName);
            OriginalQuery.Document = CreateDocumentFromSqlString(_sqlProcessor.OriginalStringAsArray());

            var itemsBinding = new Binding
            {
                Source = Queries
            };
            
            ProcessedQueries.SetBinding(ItemsControl.ItemsSourceProperty, itemsBinding);
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

        private void ProcessPasteOperation()
        {
            if (IsQueryBoxEmpty() || (!IsQueryBoxEmpty() && ConfirmOverwriteCurrentText()))
            {
                var text = Clipboard.GetText();
                OriginalQuery.Document = CreateDocumentFromSqlString(new[] { text });
                _sqlProcessor.Load(text);
                var queryData = _sqlProcessor.GetQueryProcessed(1);
                ProcessedQuery.Text = queryData;
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