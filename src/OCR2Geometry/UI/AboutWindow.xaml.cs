using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            try
            {
                Clipboard.SetText(WalletTextBox.Text);
            }
            catch (ExternalException)
            {
                MessageBox.Show(
                    this,
                    "The clipboard is currently unavailable. Please try again, or select and copy the wallet address manually.",
                    "OCR2Geometry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                this,
                "TRC20 wallet address copied to clipboard.",
                "OCR2Geometry",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
