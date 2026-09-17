using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OCR2Geometry.Models
{
    public sealed class CoordinatePoint : INotifyPropertyChanged
    {
        private int _number;
        private double _x;
        private double _y;
        private double _z;
        private bool _isXRecovered;
        private bool _isYRecovered;
        private bool _isZRecovered;

        public bool NeedsOcrReview { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }
        }

        public double X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Z
        {
            get => _z;
            set
            {
                if (_z != value)
                {
                    _z = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsXRecovered
        {
            get => _isXRecovered;
            set
            {
                if (_isXRecovered != value)
                {
                    _isXRecovered = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsYRecovered
        {
            get => _isYRecovered;
            set
            {
                if (_isYRecovered != value)
                {
                    _isYRecovered = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsZRecovered
        {
            get => _isZRecovered;
            set
            {
                if (_isZRecovered != value)
                {
                    _isZRecovered = value;
                    OnPropertyChanged();
                }
            }
        }

        public CoordinatePoint(int number, double x, double y, double z = 0.0)
        {
            _number = number;
            _x = x;
            _y = y;
            _z = z;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
