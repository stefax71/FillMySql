using System.Windows;
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
        
        public MainWindow()
        {
            // Create a Windows11LightThemeSettings object and set the color palette
            Windows11DarkThemeSettings windows11LightThemeSettings = new Windows11DarkThemeSettings();
            windows11LightThemeSettings.Palette = Windows11Palette.SteelBlue;
            //
            // // Register the theme settings and apply the theme to the main window
            SfSkinManager.RegisterThemeSettings("Windows11Dark", windows11LightThemeSettings);
            SfSkinManager.SetTheme(this, new Theme("Windows11Dark"));
            SfSkinManager.ApplyStylesOnApplication = true;
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                MessageBox.Show("Hello!","Hi there",  System.Windows.MessageBoxButton.OK);    
            }
        }
    }
}