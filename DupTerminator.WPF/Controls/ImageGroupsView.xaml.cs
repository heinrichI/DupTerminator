using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ImageGroupsView.xaml
    /// </summary>
    public partial class ImageGroupsView : UserControl
    {
        public ImageGroupsView()
        {
            InitializeComponent();
        }

       // public static readonly DependencyProperty ImageGroupsProperty =
       //DependencyProperty.Register("ImageGroups", typeof(ObservableCollection<ImageGroupViewModel>),
       //    typeof(ImageGroupsView), new PropertyMetadata(null));

       // public ObservableCollection<ImageGroupViewModel> ImageGroups
       // {
       //     get => (ObservableCollection<ImageGroupViewModel>)GetValue(ImageGroupsProperty);
       //     set => SetValue(ImageGroupsProperty, value);
       // }
    }
}
