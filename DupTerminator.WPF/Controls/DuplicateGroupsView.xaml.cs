using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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
using DupTerminator.WPF.Model;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for DuplicateGroupsView.xaml
    /// </summary>
    public partial class DuplicateGroupsView : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DuplicateGroupsView()
        {
            InitializeComponent();
            //FilesGrid.DataContext = this;
      
        }


        private ObservableCollection<FileRow> _fileRows = new();
        public ObservableCollection<FileRow> FileRows
        {
            get => _fileRows;
            set
            {
                _fileRows = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileRows)));
            }
        }

        public ObservableCollection<DuplicateGroup> DuplicateGroups
        {
            get => (ObservableCollection<DuplicateGroup>)GetValue(DuplicateGroupsProperty);
            set
            {
                SetValue(DuplicateGroupsProperty, value);
                RefreshRows();
            }
        }

        public static readonly DependencyProperty DuplicateGroupsProperty =
            DependencyProperty.Register("DuplicateGroups", typeof(ObservableCollection<DuplicateGroup>),
            typeof(DuplicateGroupsView), new PropertyMetadata(null, OnDuplicateGroupsChanged));

        private static void OnDuplicateGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DuplicateGroupsView view)
            {
                // Отписываемся от предыдущей коллекции
                if (view.DuplicateGroups != null)
                {
                    view.DuplicateGroups.CollectionChanged -= view.OnGroupsCollectionChanged;
                }

                 view.DuplicateGroups = e.NewValue as ObservableCollection<DuplicateGroup>;
                //    view.DuplicateGroups.Add(new DuplicateGroup("12345", new List<ExtendedFileInfo>
                //{
                //    new ExtendedFileInfo() { Name = "file1.txt", Size = 123, Path = @"C:\" }
                //}));

                // Подписываемся на новую коллекцию
                if (view.DuplicateGroups != null)
                {
                    view.DuplicateGroups.CollectionChanged += view.OnGroupsCollectionChanged;
                    //control.LoadData(view.DuplicateGroups);  // Загружаем начальные данные
                    view.RefreshRows();
                }
                else
                {
                    view.FileRows.Clear();  // Очищаем, если коллекция null
                }
            }
        }

        private void OnGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshRows();
        }

        private void RefreshRows()
        {
            FileRows.Clear();
            if (DuplicateGroups == null) return;

            foreach (var group in DuplicateGroups)
            {
                foreach (var file in group.Files)
                {
                    FileRows.Add(new FileRow(file, group));
                }
            }
        }

        private void FilesGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //var column = e.Column;
            //var propertyName = column.SortMemberPath;

            //// Для сортировки по колонкам группы
            //if (propertyName.StartsWith("Group."))
            //{
            //    e.Handled = true;
            //    SortGroups(propertyName);
            //}

            // 1. Toggle the direction shown on the column header
            var direction = e.Column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            e.Column.SortDirection = direction;
            e.Handled = true;          // We’re handling it ourselves now

            // 2. Take the view that the grid is actually using
            var view = CollectionViewSource.GetDefaultView(FilesGrid.ItemsSource) as ListCollectionView;
            if (view == null) return;

            // 3. Clear whatever previous sorting we had
            view.SortDescriptions.Clear();

            // 4. Sort first by group (so all items belonging to the same group stay together)
            view.SortDescriptions.Add(new SortDescription(nameof(FileRow.GroupKey), ListSortDirection.Ascending));

            // 5. Then sort by the column that the user clicked
            var memberPath = e.Column.SortMemberPath;   // e.g. "File.Size"
            view.SortDescriptions.Add(new SortDescription(memberPath, direction));

            // 6. Refresh the view to apply the changes
            view.Refresh();
        }

        private void SortGroups(string propertyName)
        {
            var sorted = propertyName switch
            {
                "Group.Name" => DuplicateGroups.OrderBy(g => g.Name),
                "Group.Checksum" => DuplicateGroups.OrderBy(g => g.Checksum),
                _ => DuplicateGroups.OrderBy(g => g.Files.First().Size)
            };

            //DuplicateGroups = new ReadOnlyObservableCollection<DuplicateGroup>(
            //new ObservableCollection<DuplicateGroup>(sorted));
            DuplicateGroups = new ObservableCollection<DuplicateGroup>(sorted);
        }
    }

    public class FileRow
    {
        public ExtendedFileInfo File { get; }
        public DuplicateGroup Group { get; }
        public string GroupKey => $"{Group.Checksum}_{Group.Name}";

        public FileRow(ExtendedFileInfo file, DuplicateGroup group)
        {
            File = file;
            Group = group;
        }
    }

    public class GroupIndexToBrushConverter : IValueConverter
    {
        public Brush EvenBrush { get; set; } = Brushes.White;
        public Brush OddBrush { get; set; } = Brushes.LightGray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DataGridRow row ||
                row.DataContext is not FileRow fileRow)
                return null;

            var itemsControl = ItemsControl.ItemsControlFromItemContainer(row);
            if (itemsControl == null) return EvenBrush;

            //var index = itemsControl.Items
            //    .Cast<CollectionViewGroup>()
            //    .ToList()
            //    .FindIndex(g => g.Items.Cast<FileRow>().First().Group == fileRow.Group);
            int index = 0;

            return index % 2 == 0 ? EvenBrush : OddBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
