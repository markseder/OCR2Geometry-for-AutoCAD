using System;
using System.Collections.Generic;
using System.Windows;
using OCR2Geometry.AutoCAD;
using OCR2Geometry.Models;

namespace OCR2Geometry.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateTestPoints_Click(object sender, RoutedEventArgs e)
        {
            var points = new List<CoordinatePoint>
            {
                new CoordinatePoint(1, 512345.23, 6876543.11),
                new CoordinatePoint(2, 512351.86, 6876551.42),
                new CoordinatePoint(3, 512360.14, 6876567.30)
            };

            try
            {
                PointCreator.CreatePoints(points);
                MessageBox.Show(
                    "Three test points and labels were created in Model Space.",
                    "OCR2Geometry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "OCR2Geometry error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
