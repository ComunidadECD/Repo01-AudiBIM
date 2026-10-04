using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using BIMQualityAuditor.UI.ViewModels;

namespace BIMQualityAuditor.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            LoadAppIcon();
        }

        private void LoadAppIcon()
        {
            try
            {
                string asmPath = Assembly.GetExecutingAssembly().Location;
                string asmDir = Path.GetDirectoryName(asmPath) ?? string.Empty;

                string blackPngPath = Path.Combine(asmDir, "ico_black.png");
                string pngPath = Path.Combine(asmDir, "ico.png");
                string icoPath = Path.Combine(asmDir, "AudiBIM.ico");

                string targetLogo = File.Exists(blackPngPath) ? blackPngPath : pngPath;

                if (File.Exists(targetLogo))
                {
                    var logoImg = new BitmapImage(new Uri(targetLogo, UriKind.Absolute));
                    ImgLogo.Source = logoImg;
                    Icon = logoImg;
                }
                else if (File.Exists(icoPath))
                {
                    Icon = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
                }
            }
            catch
            {
                // Ignore icon load failure gracefully without crashing WPF window
            }
        }

        private void ResultsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.DataGrid dg && dg.DataContext is RuleTabViewModel tabVm)
            {
                tabVm.SelectedElements.Clear();
                foreach (var item in dg.SelectedItems.OfType<BIMQualityAuditor.Models.ElementAuditResult>())
                {
                    tabVm.SelectedElements.Add(item);
                }
            }
        }
    }
}
