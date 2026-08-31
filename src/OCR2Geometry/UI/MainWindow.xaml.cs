using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using OCR2Geometry.AutoCAD;
using OCR2Geometry.Export;
using OCR2Geometry.Models;

namespace OCR2Geometry.UI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<CoordinatePoint> Points { get; }

        public MainWindow()
        {
            Points = new ObservableCollection<CoordinatePoint>
            {
                new CoordinatePoint(1, 512345.23, 6876543.11),
                new CoordinatePoint(2, 512351.86, 6876551.42),
                new CoordinatePoint(3, 512360.14, 6876567.30)
            };

            InitializeComponent();
            DataContext = this;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            Points.Add(new CoordinatePoint(startNumber + Points.Count, 0.0, 0.0));
            RenumberPoints(startNumber);
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = PointsGrid.SelectedItems.Cast<CoordinatePoint>().ToList();
            foreach (var point in selected)
            {
                Points.Remove(point);
            }

            int startNumber;
            if (TryGetStartNumber(out startNumber, false))
            {
                RenumberPoints(startNumber);
            }
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

            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            double textHeight;
            if (!TryParsePositiveDouble(TextHeightTextBox.Text, out textHeight))
            {
                ShowError("Text height must be a positive number.");
                return;
            }

            RenumberPoints(startNumber);

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

            int startNumber;
            if (!TryGetStartNumber(out startNumber))
            {
                return;
            }

            RenumberPoints(startNumber);

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

        private void RenumberPoints(int startNumber)
        {
            for (var i = 0; i < Points.Count; i++)
            {
                Points[i].Number = startNumber + i;
            }

            PointsGrid.Items.Refresh();
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
