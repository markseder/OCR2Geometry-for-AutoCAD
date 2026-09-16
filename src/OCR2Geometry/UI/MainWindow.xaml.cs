using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OCR2Geometry.AutoCAD;
using OCR2Geometry.Export;
using OCR2Geometry.Import;
using OCR2Geometry.Models;
using OCR2Geometry.OCR;

namespace OCR2Geometry.UI
{
    public partial class MainWindow : Window
    {
        private readonly IOcrEngine _ocrEngine;
        private string _selectedImagePath;

        public ObservableCollection<CoordinatePoint> Points { get; }

        public MainWindow()
        {
            Points = new ObservableCollection<CoordinatePoint>
            {
                new CoordinatePoint(1, 512345.23, 6876543.11),
                new CoordinatePoint(2, 512351.86, 6876551.42),
                new CoordinatePoint(3, 512360.14, 6876567.30)
            };

            _ocrEngine = new UnavailableOcrEngine();

            InitializeComponent();
            DataContext = this;
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select coordinate table image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _selectedImagePath = dialog.FileName;
            SelectedImageTextBox.Text = _selectedImagePath;
            PreviewImageButton.IsEnabled = true;
            RecognizeImageButton.IsEnabled = true;

            LoadImagePreview(_selectedImagePath);
            ImportStatusText.Text = "Image selected: " + Path.GetFileName(_selectedImagePath);
        }

        private void PreviewImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                ShowError("Select an image first.");
                return;
            }

            LoadImagePreview(_selectedImagePath);
        }

        private void RecognizeImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                ShowError("Select an image first.");
                return;
            }

            if (!_ocrEngine.IsAvailable)
            {
                ShowError(
                    "Image selection and preview are working. " +
                    "The local OCR engine is the next v0.4 step and is not connected in this build yet.");
                return;
            }

            try
            {
                var result = _ocrEngine.Recognize(_selectedImagePath);
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    ShowError("OCR did not return any text.");
                    return;
                }

                ImportCoordinateText(result.Text, Path.GetFileName(_selectedImagePath) + " / " + result.EngineName);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LoadImagePreview(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                ImagePreview.Source = bitmap;
                PreviewPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ImagePreview.Source = null;
                PreviewPlaceholder.Visibility = Visibility.Visible;
                ShowError("Could not preview the selected image: " + ex.Message);
            }
        }

        private void PasteCoordinates_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                ShowError("Clipboard does not contain text.");
                return;
            }

            ImportCoordinateText(Clipboard.GetText(), "clipboard");
        }

        private void ImportTextFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import coordinates",
                Filter = "Coordinate files (*.csv;*.txt)|*.csv;*.txt|CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                ImportCoordinateText(File.ReadAllText(dialog.FileName), Path.GetFileName(dialog.FileName));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ClearTable_Click(object sender, RoutedEventArgs e)
        {
            Points.Clear();
            ImportStatusText.Text = "Table cleared";
        }

        private void ImportCoordinateText(string text, string sourceName)
        {
            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            var result = TextCoordinateParser.Parse(text, startNumber);
            if (result.Points.Count == 0)
            {
                ShowError("No coordinate rows were recognized in " + sourceName + ".");
                return;
            }

            Points.Clear();
            foreach (var point in result.Points)
            {
                Points.Add(point);
            }

            PointsGrid.Items.Refresh();

            if (result.InvalidLineNumbers.Count > 0)
            {
                var preview = string.Join(", ", result.InvalidLineNumbers.Take(8));
                if (result.InvalidLineNumbers.Count > 8)
                {
                    preview += ", ...";
                }

                ImportStatusText.Text = Points.Count + " imported; skipped lines: " + preview;
            }
            else
            {
                ImportStatusText.Text = Points.Count + " coordinate rows imported from " + sourceName;
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            Points.Add(new CoordinatePoint(GetNextPointNumber(startNumber), 0.0, 0.0));
            PointsGrid.Items.Refresh();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = PointsGrid.SelectedItems.Cast<CoordinatePoint>().ToList();
            foreach (var point in selected)
            {
                Points.Remove(point);
            }

            PointsGrid.Items.Refresh();
        }

        private void SwapXY_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits();

            foreach (var point in Points)
            {
                var x = point.X;
                point.X = point.Y;
                point.Y = x;
            }

            PointsGrid.Items.Refresh();
        }

        private void CreatePoints_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits();

            if (Points.Count == 0)
            {
                ShowError("The coordinate table is empty.");
                return;
            }

            double textHeight;
            if (!TryParsePositiveDouble(TextHeightTextBox.Text, out textHeight))
            {
                ShowError("Text height must be a positive number.");
                return;
            }

            try
            {
                PointCreator.CreatePoints(Points, textHeight, textHeight);
                MessageBox.Show(
                    Points.Count + " point(s) and labels were created in Model Space.",
                    "OCR2Geometry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits();

            if (Points.Count == 0)
            {
                ShowError("The coordinate table is empty.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export coordinates to CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = "coordinates.csv"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                CsvExporter.Export(dialog.FileName, Points);
                MessageBox.Show(
                    "CSV exported successfully.",
                    "OCR2Geometry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void CommitGridEdits()
        {
            PointsGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
            PointsGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        }

        private bool TryGetStartNumber(out int startNumber, bool showError = true)
        {
            if (int.TryParse(StartNumberTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out startNumber) && startNumber >= 0)
            {
                return true;
            }

            if (showError)
            {
                ShowError("Start number must be a whole number greater than or equal to 0.");
            }

            return false;
        }

        private int GetNextPointNumber(int startNumber)
        {
            if (Points.Count == 0)
            {
                return startNumber;
            }

            return Points.Max(p => p.Number) + 1;
        }

        private static bool TryParsePositiveDouble(string value, out double result)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0)
            {
                return true;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) && result > 0)
            {
                return true;
            }

            return false;
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                "OCR2Geometry error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
