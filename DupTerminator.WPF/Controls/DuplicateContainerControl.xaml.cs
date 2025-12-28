using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for DuplicateContainerView.xaml
    /// </summary>
    public partial class DuplicateContainerControl : UserControl
    {
        public DuplicateContainerControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
       DependencyProperty.Register("ItemsSource", typeof(IEnumerable),
           typeof(DuplicateContainerControl), new PropertyMetadata(null, OnItemsSourceChanged));


        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private ICollectionView _collectionView;

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DuplicateContainerControl control)
            {
                control._collectionView = CollectionViewSource.GetDefaultView(control.ItemsSource);
                if (control._collectionView is not null)
                    control._collectionView.Filter = control.FilterItems;
                control.listView.ItemsSource = control._collectionView;
            }
        }

        private bool FilterItems(object item)
        {
            if (item is not DuplicateContainer container) return false;

            var filter = txtFirstFilter.Text;

            if (string.IsNullOrEmpty(filter))
                return true;

            var firstMatch = container.FirstInfo.Path?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
            var secondMatch = container.SecondInfo.Path?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;

            return firstMatch || secondMatch;
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            _collectionView?.Refresh();
        }

        // 1️⃣  Handle double‑click on a ListView item
        private void FirstDiffrentFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listView)
            {
                // Grab the selected container
                if (!(listView.SelectedItem is SimpleFileInfo sfi))
                    return;

                //// Which path to display?  
                //// If the FirstInfo contains a file, we use that; otherwise we fall back to SecondInfo
                //string path = container.FirstInfo?.Path ?? container.SecondInfo?.Path;

                //if (string.IsNullOrWhiteSpace(path))
                //    return; // nothing to show

                //// 2️⃣  Make sure we only try to open image files
                //if (!IsImageFile(path))
                //    return;

                //// 3️⃣  Show the viewer window
                //var viewer = new ImageViewerWindow(path)
                //{
                //    Owner = Window.GetWindow(this) ?? Application.Current.MainWindow
                //};
                //viewer.Show();
            }
        }
    }
}
