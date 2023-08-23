using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using Syncfusion.SfSkinManager;
using Syncfusion.Themes.Windows11Dark.WPF;

namespace FillMySQL
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SqlProcessor _sqlProcessor ;
        
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
            var document = new FlowDocument();
                
            foreach (var currentString in _sqlProcessor.OriginalStringAsArray())
            {
                var par = new Paragraph(new Run(currentString))
                {
                    Margin = new Thickness(0)
                };
                document.Blocks.Add(par);
            }
            OriginalQuery.Document = document;
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