using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using static System.Net.Mime.MediaTypeNames;
using static DupTerminator.WPF.ViewModel.SettingsViewModel;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ListViewControl.xaml
    /// </summary>
    public partial class ListViewControl2 : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GridViewColumnHeader _listViewSortCol = null;
        private ListSortDirection _listViewSortDir = ListSortDirection.Ascending;

        public ListViewControl2()
        {
            InitializeComponent();

            // Add to your UserControl constructor
            var cvs = (CollectionViewSource)Resources["GroupedFiles"];
            cvs.IsLiveFilteringRequested = true;
            cvs.LiveFilteringProperties.Add("Path");  // Your filtering property
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

        public ObservableCollection<ExtendedFileInfo> ExtendedFileInfos { get; } = new ObservableCollection<ExtendedFileInfo>();
        //private ObservableCollection<ExtendedFileInfo> _extendedFileInfos;
        //public ObservableCollection<ExtendedFileInfo> ExtendedFileInfos
        //{
        //    get => _extendedFileInfos;
        //    set { _extendedFileInfos = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExtendedFileInfos))); }
        //}

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

            //// Create new collection instead of modifying existing
            //var newCollection = new ObservableCollection<ExtendedFileInfo>();

            //if (DuplicateGroups != null)
            //{
            //    foreach (var group in DuplicateGroups)
            //    {
            //        // Use AddRange if available, or cache in List first
            //        var groupFiles = group.Files.ToList();
            //        foreach (var file in groupFiles)
            //        {
            //            newCollection.Add(file);
            //        }
            //    }
            //}

            //// Atomic replacement instead of Clear()+Add()
            //ExtendedFileInfos = newCollection;
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExtendedFileInfos)));

            // Suspend UI notifications during bulk update
            //var collection = (IList)ExtendedFileInfos;
            //var binding = BindingOperations.GetBindingExpression(this, ItemsControl.ItemsSourceProperty);
            //binding?.ParentBinding.ProvideValue(null); // Bypass binding updates temporarily

            //try
            //{
            //    collection.Clear();

            //    if (DuplicateGroups == null) return;

            //    var newItems = DuplicateGroups
            //        .SelectMany(group => group.Files)
            //        .ToList();

            //    // Bulk add using reflection for ObservableCollection<T>
            //    var method = typeof(ObservableCollection<ExtendedFileInfo>)
            //        .GetMethod("AddRange", BindingFlags.Instance | BindingFlags.Public);

            //    if (method is null)
            //    {
            //        var methodAdd = typeof(ObservableCollection<ExtendedFileInfo>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
            //        foreach (var item in newItems)
            //        {
            //            methodAdd.Invoke(ExtendedFileInfos, new object[] { item });
            //        }
            //    }
            //    else
            //    {
            //        method?.Invoke(ExtendedFileInfos, new object[] { newItems });
            //    }
            //}
            //finally
            //{
            //    // Restore binding notifications
            //    BindingOperations.SetBinding(
            //        FilesListView,
            //        ItemsControl.ItemsSourceProperty,
            //        new Binding { Source = CollectionViewSource.GetDefaultView(ExtendedFileInfos) }
            //    );
            //}
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

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            var cvs = (CollectionViewSource)Resources["GroupedFiles"];
            if (string.IsNullOrEmpty(text))
            {
                cvs.View.Filter = null;
            }
            else
            {
                cvs.View.Filter = FilterItems;
            }
            cvs.View.Refresh();
        }

        private bool FilterItems(object item)
        {
            if (item is not ExtendedFileInfo efi)
                return false;

            var filter = txtFirstFilter.Text;

            if (string.IsNullOrEmpty(filter))
                return true;

            var firstMatch = efi.Path.Contains(filter, StringComparison.OrdinalIgnoreCase);

            return firstMatch;
        }

        //private void OnFilterItems(object sender, FilterEventArgs e)
        //{
        //    // This method will be called for each item in the collection
        //    if (e.Item is ExtendedFileInfo fileInfo)
        //    {
        //        // Apply your filter logic here
        //        e.Accepted = FilterItems(fileInfo);
        //    }
        //    else
        //    {
        //        // If it's not an ExtendedFileInfo, show it (e.g., group headers)
        //        e.Accepted = true;
        //    }
        //}
    }
}
