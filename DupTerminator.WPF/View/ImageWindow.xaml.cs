using System;
using System.Collections.Generic;
using System.Linq;
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
        public ImageWindow(BitmapImage image)
        {
            InitializeComponent();

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
    }
}
