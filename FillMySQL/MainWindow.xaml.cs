using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using Syncfusion.SfSkinManager;
using Syncfusion.Themes.Windows11Dark.WPF;

namespace FillMySQL
{
    // Per il resize https://markheath.net/post/creating-resizable-shape-controls-in#:~:text=In%20WPF%2C%20you%20can%20create,into%20account%20the%20stroke%20thickness.d
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly SqlProcessor _sqlProcessor ;
        public ObservableCollection<string> _queries => _sqlProcessor.Queries;
        public MainWindow()
        {
            _sqlProcessor = new SqlProcessor();
            InitializeComponent();
        }

        private void LoadFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != true) return;
            _sqlProcessor.LoadFile(openFileDialog.FileName);
            OriginalQuery.Document = CreateDocumentFromSqlString();

            Binding itemsBinding = new Binding();
            itemsBinding.Source = _queries;
            ProcessedQueries.SetBinding(ItemsControl.ItemsSourceProperty, itemsBinding);
        }

        private FlowDocument CreateDocumentFromSqlString()
        {
            var document = new FlowDocument();

            foreach (var currentString in _sqlProcessor.OriginalStringAsArray())
            {
                var par = new Paragraph(new Run(currentString))
                {
                    Margin = new Thickness(0)
                };
                document.Blocks.Add(par);
            }
            return document;
        }

        private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OriginalQuery.Width = MainGrid.Width - 20;
        }

        private void GoToNextQuery_OnClick(object sender, RoutedEventArgs e)
        {
            
            throw new System.NotImplementedException();
        }
    }
}