using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.View;
using DupTerminator.WPF.ViewModel;
using static System.Net.Mime.MediaTypeNames;
using static DupTerminator.WPF.ViewModel.SettingsViewModel;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ListViewControl.xaml
    /// </summary>
    public partial class ListViewControl2 : UserControl, INotifyPropertyChanged
    {
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChangedEvent([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private GridViewColumnHeader _listViewSortCol = null;
        private ListSortDirection _listViewSortDir = ListSortDirection.Ascending;

        // --------------------------------------------------------------------
        //  Commands
        // --------------------------------------------------------------------
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand SelectAllInFolderCommand { get; }
        public ICommand HideGroupCommand { get; }

        public ICommand DoubleClickCommand { get; }

        // --------------------------------------------------------------------
        //  Selected count (for status bar)
        // --------------------------------------------------------------------
        private int _selectedItemsCount;
        public int SelectedItemsCount
        {
            get => _selectedItemsCount;
            private set
            {
                if (_selectedItemsCount != value)
                {
                    _selectedItemsCount = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        // --------------------------------------------------------------------
        //  Constructor
        // --------------------------------------------------------------------
        public ListViewControl2()
        {
            InitializeComponent();

            // Add to your UserControl constructor
            //var cvs = (CollectionViewSource)Resources["GroupedFiles"];
            //cvs.IsLiveFilteringRequested = true;
            //cvs.LiveFilteringProperties.Add("Path");  // Your filtering property

            // Commands
            SelectAllCommand = new RelayCommand(_ => SelectAll());
            DeselectAllCommand = new RelayCommand(_ => DeselectAll());
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedItemsCount > 0);
            SelectAllInFolderCommand = new RelayCommand(SelectAllInThisFolder, _ => FilesListView.SelectedItem is ExtendedFileInfoViewModel);
            HideGroupCommand = new RelayCommand(HideGroup, _ => FilesListView.SelectedItem is ExtendedFileInfoViewModel);
            DoubleClickCommand = new RelayCommand(ExecuteDoubleClick, CanExecuteDoubleClick);

            // Listen to collection changes so we can attach PropertyChanged handlers
            ExtendedFileInfos.CollectionChanged += ExtendedFileInfos_CollectionChanged;

            // More reliable F5 handling
            this.PreviewKeyDown += ListViewControl2_PreviewKeyDown;

            // Attach handlers after control is loaded
            this.Loaded += ListViewControl2_Loaded;
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

        public ObservableCollection<ExtendedFileInfoViewModel> ExtendedFileInfos { get; } = new ObservableCollection<ExtendedFileInfoViewModel>();
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
                    DuplicateGroupViewModel duplicateGroupViewModel = new DuplicateGroupViewModel();
                    foreach (var file in group.Files)
                    {
                        var efi = new ExtendedFileInfoViewModel(file, duplicateGroupViewModel, group.Checksum);
                        duplicateGroupViewModel.Files.Add(efi);
                        ExtendedFileInfos.Add(efi);
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

        private void ListViewControl2_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to ListView when control loads
            FilesListView.Focus();
        }



        private void ListViewControl2_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (SelectAllInFolderCommand.CanExecute(null))
                {
                    SelectAllInFolderCommand.Execute(null);
                    e.Handled = true;
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
            if (item is not ExtendedFileInfoViewModel efi)
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

        // --------------------------------------------------------------------
        //  Collection change handling
        // --------------------------------------------------------------------
        private void ExtendedFileInfos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Attach to new items
            if (e.NewItems != null)
                foreach (ExtendedFileInfoViewModel item in e.NewItems)
                    item.PropertyChanged += FileInfo_PropertyChanged;

            // Detach from removed items
            if (e.OldItems != null)
                foreach (ExtendedFileInfoViewModel item in e.OldItems)
                    item.PropertyChanged -= FileInfo_PropertyChanged;

            // Re‑count
            UpdateSelectedCount();
        }

        private void FileInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExtendedFileInfoViewModel.IsSelected))
                UpdateSelectedCount();
        }

        // --------------------------------------------------------------------
        //  Helper – count selected items
        // --------------------------------------------------------------------
        private void UpdateSelectedCount()
        {
            SelectedItemsCount = ExtendedFileInfos.Count(f => f.IsSelected);
        }

        // --------------------------------------------------------------------
        //  Command handlers
        // --------------------------------------------------------------------
        private void SelectAll()
        {
            foreach (var f in ExtendedFileInfos) f.IsSelected = true;
        }

        private void DeselectAll()
        {
            foreach (var f in ExtendedFileInfos) f.IsSelected = false;
        }

        private void DeleteSelected()
        {
            // Remove from the collection – the UI will update automatically
            var toDelete = ExtendedFileInfos.Where(f => f.IsSelected).ToList();
            foreach (var f in toDelete)
                FileUtils.MoveToRecycleBin(f.Path);
        }

        private void SelectAllInThisFolder(object parameter)
        {
            if (FilesListView.SelectedItem is ExtendedFileInfoViewModel sel)
            {
                string folder = System.IO.Path.GetDirectoryName(sel.Path) ?? string.Empty;
                foreach (var f in ExtendedFileInfos)
                    f.IsSelected = System.IO.Path.GetDirectoryName(f.Path) == folder;
            }
        }

        private void HideGroup(object parameter)
        {
            if (FilesListView.SelectedItem is ExtendedFileInfoViewModel sel)
            {
                var toDelete = ExtendedFileInfos.Where(f => f.CheckSum == sel.CheckSum).ToList();
                foreach (var toDeleteItem in toDelete)
                {
                    ExtendedFileInfos.Remove(toDeleteItem);
                }
            }
        }

        // --------------------------------------------------------------------
        //  Double‑click command logic
        // --------------------------------------------------------------------
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Prevent event from bubbling up if we handle it
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Get the clicked item
            if (sender is ListViewItem item && item.Content is ExtendedFileInfoViewModel efi)
            {
                if (DoubleClickCommand.CanExecute(efi))
                {
                    DoubleClickCommand.Execute(efi);
                    e.Handled = true;
                }
            }
        }

        private bool CanExecuteDoubleClick(object? parameter)
        {
            // Only allow execution when a file is selected
            return parameter is ExtendedFileInfoViewModel;
        }

        private void ExecuteDoubleClick(object? parameter)
        {
            if (parameter is ExtendedFileInfoViewModel efi)
            {
                // Example action: open the file with the default program
                try
                {
                    if (System.IO.File.Exists(efi.Path))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = efi.Path,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        string archivePath = GetArchiveFilePathByExtension(efi.Path);
                        if (System.IO.File.Exists(archivePath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = archivePath,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle errors (e.g., file not found, no default program)
                    MessageBox.Show($"Could not open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetArchiveFilePathByExtension(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            string[] parts = fullPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            var archiveSegments = parts.Where(p => p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                                   p.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase) ||
                                                   p.EndsWith(".cbr", StringComparison.OrdinalIgnoreCase) ||
                                                   p.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) ||
                                                   p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

            if (archiveSegments.Any())
            {
                string firstArchive = archiveSegments.First();

                int index = Array.IndexOf(parts, firstArchive);
                if (index >= 0)
                {
                    return string.Join("\\", parts.Take(index + 1));
                }
            }
            return null; // No archive found
        }


        public static string CutPathToLastDoubleSlash(string path)
        {
            string separator = "//";
            // Find the index of the last occurrence of the separator
            int lastIndex = path.LastIndexOf(separator);

            // Check if the separator was found
            if (lastIndex != -1)
            {
                // Use Substring to get the part of the string from the start up to the index found
                return path.Substring(0, lastIndex);
            }
            else
            {
                // If the separator is not found, return the original string or handle as needed
                return path;
            }
        }
    }
}
