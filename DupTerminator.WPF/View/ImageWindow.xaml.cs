using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DupTerminator.WPF.View
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChangedEvent([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ImageWindow(BitmapImage image, ulong size)
        {
            InitializeComponent();

            // Set the DataContext to this instance
            DataContext = this;

            imgPreview.Source = image;
            if (image.IsDownloading)
            {
                image.DownloadCompleted += (s, e) => UpdateResolutionText(image);
                image.DecodeFailed += (s, e) => UpdateResolutionText(null);
            }
            else
            {
                UpdateResolutionText(image);
            }
            Size = size;
        }

        private void UpdateResolutionText(BitmapImage image)
        {
            if (image != null && image.PixelWidth > 0 && image.PixelHeight > 0)
            {
                txtResolution.Text = $"Resolution: {image.PixelWidth} × {image.PixelHeight}";
            }
            else
            {
                txtResolution.Text = "Resolution: N/A";
            }
        }

        private ulong _size;
        public ulong Size
        {
            get => _size;
            set { _size = value; RaisePropertyChangedEvent(); }
        }
    }
}
