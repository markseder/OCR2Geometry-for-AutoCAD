using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private string _ocrDetails = string.Empty;

        public ObservableCollection<CoordinatePoint> Points { get; }

        public MainWindow()
        {
            Points = new ObservableCollection<CoordinatePoint>();
            _ocrEngine = new TesseractOcrEngine();

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

            SetSelectedImage(dialog.FileName, "Image selected: " + Path.GetFileName(dialog.FileName));
        }

        private void PasteImage_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage())
            {
                ShowError("Clipboard does not contain an image. Use Win+Shift+S to capture the table, then click Paste image.");
                return;
            }

            try
            {
                var image = Clipboard.GetImage();
                if (image == null)
                {
                    ShowError("Could not read the image from the clipboard.");
                    return;
                }

                var tempDirectory = Path.Combine(Path.GetTempPath(), "OCR2Geometry");
                Directory.CreateDirectory(tempDirectory);
                var tempPath = Path.Combine(tempDirectory, "clipboard_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (var stream = File.Create(tempPath))
                {
                    encoder.Save(stream);
                }

                SetSelectedImage(tempPath, "Image pasted from clipboard");
            }
            catch (Exception ex)
            {
                ShowError("Could not paste the clipboard image: " + ex.Message);
            }
        }

        private void SetSelectedImage(string imagePath, string status)
        {
            _ocrDetails = string.Empty;
            OcrDetailsButton.IsEnabled = false;
            _selectedImagePath = imagePath;
            SelectedImageTextBox.Text = imagePath;
            PreviewImageButton.IsEnabled = true;
            RecognizeImageButton.IsEnabled = true;
            LoadImagePreview(imagePath);
            ImportStatusText.Text = status;
        }

        private void PreviewImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                ShowError("Select or paste an image first.");
                return;
            }

            LoadImagePreview(_selectedImagePath);
        }

        private void RecognizeImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
            {
                ShowError("Select or paste an image first.");
                return;
            }

            if (!_ocrEngine.IsAvailable)
            {
                ShowError("The local OCR engine is not available.");
                return;
            }

            try
            {
                _ocrDetails = string.Empty;
                OcrDetailsButton.IsEnabled = false;
                ImportStatusText.Text = "Recognizing image with " + _ocrEngine.Name + "...";
                var result = _ocrEngine.Recognize(_selectedImagePath,
                    OcrLayoutComboBox.SelectedIndex == 0 ? 4 : OcrLayoutComboBox.SelectedIndex == 3 ? 2 : 3,
                    OcrLayoutComboBox.SelectedIndex <= 1, (OcrMode)OcrModeComboBox.SelectedIndex);
                _ocrDetails = result.Diagnostics + "\r\nSelected OCR text:\r\n" + result.Text;
                OcrDetailsButton.IsEnabled = true;
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    ShowError("OCR did not return coordinate text. Open OCR details. For Table cells, capture the complete grid or try Text mode.");
                    ImportStatusText.Text = "OCR returned no text";
                    return;
                }

                ImportCoordinateText(
                    result.Text,
                    Path.GetFileName(_selectedImagePath) + " / " + result.EngineName,
                    true);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                ImportStatusText.Text = "OCR failed";
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

        private void ImportCoordinateText(string text, string sourceName, bool isOcr = false)
        {
            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            var result = isOcr
                ? TextCoordinateParser.ParseOcr(text, startNumber,
                    OcrLayoutComboBox.SelectedIndex == 0 ? 4 : OcrLayoutComboBox.SelectedIndex == 3 ? 2 : 3,
                    OcrLayoutComboBox.SelectedIndex <= 1)
                : TextCoordinateParser.Parse(text, startNumber);

            if (isOcr)
            {
                _ocrDetails += "\r\nParsing: " + result.Points.Count + " accepted; " + result.InvalidLineNumbers.Count + " skipped.\r\n"
                    + string.Join("\r\n", result.InvalidLineDetails);
            }

            if (result.Points.Count == 0)
            {
                ImportStatusText.Text = "No rows imported; existing table unchanged. Open OCR details.";
                ShowError("No coordinate rows were recognized in " + sourceName +
                    (isOcr ? ". Check the OCR column order and image quality." : "."));
                return;
            }

            Points.Clear();
            foreach (var point in result.Points)
            {
                point.NeedsOcrReview = isOcr;
                Points.Add(point);
            }

            PointsGrid.Items.Refresh();

            var recoveryText = result.RecoveredDecimalCount > 0
                ? "; recovered decimal separators: " + result.RecoveredDecimalCount
                : string.Empty;
            if (isOcr) recoveryText += "; verify highlighted OCR rows";

            if (result.InvalidLineNumbers.Count > 0)
            {
                var preview = string.Join(", ", result.InvalidLineNumbers.Take(8));
                if (result.InvalidLineNumbers.Count > 8)
                {
                    preview += ", ...";
                }

                ImportStatusText.Text = Points.Count + " imported; skipped lines: " + preview + recoveryText;
            }
            else
            {
                ImportStatusText.Text = Points.Count + " coordinate rows imported from " + sourceName + recoveryText;
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

                var recovered = point.IsXRecovered;
                point.IsXRecovered = point.IsYRecovered;
                point.IsYRecovered = recovered;
            }

            PointsGrid.Items.Refresh();
        }

        private void PointsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            var point = e.Row.Item as CoordinatePoint;
            if (point == null)
            {
                return;
            }

            var header = e.Column.Header as string;
            if (string.Equals(header, "X", StringComparison.Ordinal))
            {
                point.IsXRecovered = false;
            }
            else if (string.Equals(header, "Y", StringComparison.Ordinal))
            {
                point.IsYRecovered = false;
            }
            else if (string.Equals(header, "Z", StringComparison.Ordinal))
            {
                point.IsZRecovered = false;
            }
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

        private void OcrDetails_Click(object sender, RoutedEventArgs e)
        {
            var textBox = new TextBox { Text = _ocrDetails, IsReadOnly = true, AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(12) };
            new Window { Title = "OCR details — raw text and skipped rows", Owner = this,
                Width = 820, Height = 550, Content = textBox, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
        }

        private void AboutDonate_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void CommitGridEdits()
        {
            PointsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            PointsGrid.CommitEdit(DataGridEditingUnit.Row, true);
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
