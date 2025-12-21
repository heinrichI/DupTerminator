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
    public partial class ListViewControl : UserControl
    {
        private string _lastSortColumn;
        private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;

        public ListViewControl()
        {
            InitializeComponent();
            ExtendedFileInfos = new ObservableCollection<ExtendedFileInfo>();
        }


        public static readonly DependencyProperty DuplicateGroupsProperty =
          DependencyProperty.Register(
              "DuplicateGroups",
              typeof(ObservableCollection<DuplicateGroup>),
              typeof(ListViewControl),
              new PropertyMetadata(null, OnDuplicateGroupsChanged));

        public ObservableCollection<DuplicateGroup> DuplicateGroups
        {
            get { return (ObservableCollection<DuplicateGroup>)GetValue(DuplicateGroupsProperty); }
            set { SetValue(DuplicateGroupsProperty, value); }
        }

        public ObservableCollection<ExtendedFileInfo> ExtendedFileInfos { get; set; }

        private static void OnDuplicateGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ListViewControl)d;
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

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = sender as GridViewColumnHeader;
            var column = header?.Tag as GridViewColumn;
            var propName = column?.DisplayMemberBinding?.BindingGroupName ?? header?.Content.ToString();

            if (string.IsNullOrEmpty(propName)) return;

            var view = CollectionViewSource.GetDefaultView(FilesListView.ItemsSource);
            if (view == null) return;

            // Reverse direction if clicking the same column
            if (_lastSortColumn == propName)
            {
                _lastSortDirection = _lastSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _lastSortColumn = propName;
                _lastSortDirection = ListSortDirection.Ascending;
            }

            using (view.DeferRefresh())
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("CheckSum"));

                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("CheckSum", ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(propName, _lastSortDirection));
            }

            // Update header appearance
            foreach (var col in ((GridView)FilesListView.View).Columns)
            {
                var h = col.Header as GridViewColumnHeader;
                if (h != null) h.Tag = null;
            }
            header.Tag = _lastSortDirection;
        }
    }
}
