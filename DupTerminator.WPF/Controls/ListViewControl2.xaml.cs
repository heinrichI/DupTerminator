using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ListViewControl.xaml
    /// </summary>
    public partial class ListViewControl2 : UserControl
    {
        public ObservableCollection<ExtendedFileInfo> ExtendedFileInfos { get; set; } = new ObservableCollection<ExtendedFileInfo>();

        private GridViewColumnHeader _listViewSortCol = null;
        private ListSortDirection _listViewSortDir = ListSortDirection.Ascending;

        public ListViewControl2()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DuplicateGroupsProperty =
          DependencyProperty.Register(
              "DuplicateGroups",
              typeof(ICollection<DuplicateGroup>),
              typeof(ListViewControl2),
              new PropertyMetadata(null, OnDuplicateGroupsChanged));

        public ICollection<DuplicateGroup> DuplicateGroups
        {
            get { return (ICollection<DuplicateGroup>)GetValue(DuplicateGroupsProperty); }
            set { SetValue(DuplicateGroupsProperty, value); }
        }

        private static void OnDuplicateGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ListViewControl2)d;
            control.UpdateExtendedFileInfos();
        }

        private void UpdateExtendedFileInfos()
        {
            ExtendedFileInfos.Clear();

            if (DuplicateGroups != null)
            {
                foreach (var group in DuplicateGroups)
                {
                    foreach (var file in group.Files)
                    {
                        ExtendedFileInfos.Add(file);
                    }
                }
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked != null && headerClicked.Column != null)
            {
                // Determine the property name to sort by from the column's DisplayMemberBinding
                string sortBy = (headerClicked.Column.DisplayMemberBinding as Binding)?.Path.Path ?? headerClicked.Content?.ToString();

                if (!string.IsNullOrEmpty(sortBy))
                {
                    ListSortDirection direction = ListSortDirection.Ascending;
                    if (headerClicked == _listViewSortCol && _listViewSortDir == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }

                    // Get the CollectionView from the ListView's ItemsSource
                    ICollectionView view = CollectionViewSource.GetDefaultView(FilesListView.ItemsSource);

                    // Clear existing sort descriptions and apply the new one
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription(sortBy, direction));

                    // Optional: Add secondary sort criteria if needed (e.g., by Name if Age is same)
                    // view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                    // Update tracking variables for next click
                    _listViewSortCol = headerClicked;
                    _listViewSortDir = direction;
                }
            }
        }
    }
}
