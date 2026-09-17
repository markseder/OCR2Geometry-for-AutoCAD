using System;
using System.Diagnostics;
using System.Windows;

namespace OCR2Geometry.UI
{
    public partial class AboutWindow : Window
    {
        private const string ProjectUrl = "https://github.com/markseder/OCR2Geometry-for-AutoCAD";

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProjectUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open the project page: " + ex.Message,
                    "OCR2Geometry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyWallet_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(WalletTextBox.Text);
            MessageBox.Show(
                "TRC20 wallet address copied to clipboard.",
                "OCR2Geometry",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
