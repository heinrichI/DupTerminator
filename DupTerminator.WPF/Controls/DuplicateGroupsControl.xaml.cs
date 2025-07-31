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
    /// Interaction logic for DuplicateGroupsControl.xaml
    /// </summary>
    public partial class DuplicateGroupsControl : UserControl
    {
        private ObservableCollection<FileViewModel> _items;
        public ICollectionView DataView;
        private ObservableCollection<DuplicateGroup>? _observableGroups;  // Для отслеживания коллекции

        public DuplicateGroupsControl()
        {
            InitializeComponent();
            _items = new ObservableCollection<FileViewModel>();
            DataView = CollectionViewSource.GetDefaultView(_items);
        }

        // Зависимое свойство для привязки списка групп
        public static readonly DependencyProperty GroupsProperty =
            DependencyProperty.Register("Groups", typeof(ObservableCollection<DuplicateGroup>), typeof(DuplicateGroupsControl),
                new PropertyMetadata(null, OnGroupsChanged));

        public ObservableCollection<DuplicateGroup> Groups
        {
            get { return (ObservableCollection<DuplicateGroup>)GetValue(GroupsProperty); }
            set { SetValue(GroupsProperty, value); }
        }

        private static void OnGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DuplicateGroupsControl control)
            {
                // Отписываемся от предыдущей коллекции
                if (control._observableGroups != null)
                {
                    control._observableGroups.CollectionChanged -= control.OnGroupsCollectionChanged;
                }

                control._observableGroups = e.NewValue as ObservableCollection<DuplicateGroup>;

                // Подписываемся на новую коллекцию
                if (control._observableGroups != null)
                {
                    control._observableGroups.CollectionChanged += control.OnGroupsCollectionChanged;
                    control.LoadData(control._observableGroups);  // Загружаем начальные данные
                }
                else
                {
                    control._items.Clear();  // Очищаем, если коллекция null
                }
            }
        }

        // Обработчик изменений в ObservableCollection
        private void OnGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            LoadData(_observableGroups);  // Перезагружаем данные при изменении
        }

        private void LoadData(ObservableCollection<DuplicateGroup>? groups)
        {
            if (groups == null) return;  // Проверка на null

            _items.Clear();  // Очищаем существующий список

            int groupIndex = 0;
            foreach (var group in groups)
            {
                foreach (var file in group.Files)
                {
                    _items.Add(new FileViewModel
                    {
                        File = file,
                        Group = group,
                        GroupIndex = groupIndex
                    });
                }
                groupIndex++;
            }

            // Настроить сортировку: сначала по индексу группы
            DataView.SortDescriptions.Clear();
            DataView.SortDescriptions.Add(new SortDescription("GroupIndex", ListSortDirection.Ascending));

            dataGrid.ItemsSource = DataView;  // Привязать к DataGrid
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            string sortMemberPath = e.Column.SortMemberPath;

            DataView.SortDescriptions.Clear();
            DataView.SortDescriptions.Add(new SortDescription("GroupIndex", ListSortDirection.Ascending));  // Сортировка по группе
            DataView.SortDescriptions.Add(new SortDescription(sortMemberPath, e.Column.SortDirection ?? ListSortDirection.Ascending));

            e.Handled = true;
        }

        public class FileViewModel
        {
            public ExtendedFileInfo File { get; set; }
            public DuplicateGroup Group { get; set; }
            public int GroupIndex { get; set; }
        }
    }
}
