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
    /// Interaction logic for DuplicateGroupsView2.xaml
    /// </summary>
    public partial class DuplicateGroupsView2 : UserControl
    {
        public DuplicateGroupsView2()
        {
            ExtendedFileInfos = new ObservableCollection<ExtendedFileInfo>();
            //DataContext = this;
            InitializeComponent();
        }

        public static readonly DependencyProperty DuplicateGroupsProperty =
          DependencyProperty.Register(
              "DuplicateGroups",
              typeof(ObservableCollection<DuplicateGroup>),
              typeof(DuplicateGroupsView2),
              new PropertyMetadata(null, OnDuplicateGroupsChanged));

        public ObservableCollection<DuplicateGroup> DuplicateGroups
        {
            get { return (ObservableCollection<DuplicateGroup>)GetValue(DuplicateGroupsProperty); }
            set { SetValue(DuplicateGroupsProperty, value); }
        }

        public ObservableCollection<ExtendedFileInfo> ExtendedFileInfos { get; set; }


        private static void OnDuplicateGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DuplicateGroupsView2)d;
            //control.UpdateExtendedFileInfos();

            // Отписываемся от предыдущей коллекции
            if (control.DuplicateGroups != null)
            {
                control.DuplicateGroups.CollectionChanged -= control.OnGroupsCollectionChanged;
            }

            control.DuplicateGroups = e.NewValue as ObservableCollection<DuplicateGroup>;

            // Подписываемся на новую коллекцию
            if (control.DuplicateGroups != null)
            {
                control.DuplicateGroups.CollectionChanged += control.OnGroupsCollectionChanged;
                control.UpdateExtendedFileInfos();
            }
            else
            {
                control.ExtendedFileInfos.Clear();  // Очищаем, если коллекция null
            }
        }

        private void OnGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateExtendedFileInfos();
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

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var collectionView = CollectionViewSource.GetDefaultView(FileDataGrid.ItemsSource);
            if (collectionView == null) return;

            var sortDescriptions = collectionView.SortDescriptions;

            // Get the property name to sort by
            string propertyName = e.Column.SortMemberPath ?? e.Column.Header.ToString();

            // Find existing sort description for this column
            var existingSort = sortDescriptions.FirstOrDefault(sd => sd.PropertyName == propertyName);

            // Determine new sort direction
            ListSortDirection newDirection = ListSortDirection.Ascending;
            if (existingSort.PropertyName != null)
            {
                newDirection = existingSort.Direction == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }

            // Clear all sort descriptions except the group sort
            //var groupSort = sortDescriptions.FirstOrDefault(sd => sd.PropertyName == "CheckSum");
            sortDescriptions.Clear();

            // Always keep the group sort first
            //if (groupSort.PropertyName != null)
            //{
            //    sortDescriptions.Add(groupSort);
            //}
            //else
            //{
            //    sortDescriptions.Add(new SortDescription("CheckSum", ListSortDirection.Ascending));
            //}

            // Add the new sort
            if (propertyName != "CheckSum")
            {
                sortDescriptions.Add(new SortDescription(propertyName, newDirection));
            }

            // Update column header to show sort direction
            foreach (var column in FileDataGrid.Columns)
            {
                column.SortDirection = null;
            }
            e.Column.SortDirection = newDirection;

            // Refresh the view
            collectionView.Refresh();
        }
    }
}