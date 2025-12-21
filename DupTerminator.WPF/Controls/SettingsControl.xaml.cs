using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Helper;
using DupTerminator.WPF.ViewModel;
using static DupTerminator.WPF.ViewModel.SettingsViewModel;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsBase SelectedMode
        {
            get { return (SettingsBase)GetValue(SelectedModeProperty); }
            set { SetValue(SelectedModeProperty, value); }
        }

        public static readonly DependencyProperty SelectedModeProperty =
                    DependencyProperty.Register("SelectedMode", typeof(SettingsBase),
                    typeof(SettingsControl), new PropertyMetadata(null, OnSelectedModeChanged));

        private static void OnSelectedModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SettingsControl)d;

            //control.LoadSettings();

            // Update the ViewModel's property through binding
            //BindingExpression be = control.GetBindingExpression(SelectedModeProperty);
            //be?.UpdateSource();

            //var viewModel = (SettingViewModel)control.DataContext; // Get the ViewModel
            //if (viewModel != null)
            //{
            //    viewModel.SelectedMode = (SettingsBase)e.NewValue; // Update the ViewModel's property
            //}
            //control.LoadSettings(); // Load settings after the ViewModel is updated

            // Update the ViewModel's property through binding
            BindingExpression be = control.GetBindingExpression(SettingsControl.SelectedModeProperty);
            be?.UpdateSource();

            // Load settings after the ViewModel is updated
            control.LoadSettings();
        }

        public ObservableCollection<SettingsBase> Modes
        {
            get { return (ObservableCollection<SettingsBase>)GetValue(ModesProperty); }
            set { SetValue(ModesProperty, value); }
        }

        public static readonly DependencyProperty ModesProperty =
            DependencyProperty.Register("Modes", typeof(ObservableCollection<SettingsBase>),
            typeof(SettingsControl), new PropertyMetadata(null, OnModesChanged));

        private static void OnModesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SettingsControl)d;
            control.Modes = (ObservableCollection<SettingsBase>)e.NewValue;
            control.Modes.CollectionChanged += OnModesChanged;
        }

        private static void OnModesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {

        }
        //private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    LoadSettings();
        //}

       

        private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SettingsControl)d;
            control.LoadSettings();
        }

        private ObservableCollection<SettingItem> _settingsList = new ObservableCollection<SettingItem>();
        public ObservableCollection<SettingItem> SettingsList
        {
            get => _settingsList;
            set { _settingsList = value; }
        }

        // Load properties via reflection
        private void LoadSettings()
        {
            SettingsList.Clear();
            if (SelectedMode == null) return;

            var properties = SelectedMode.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue; // Only editable properties

                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name;
                var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                var category = prop.GetCustomAttribute<CategoryAttribute>()?.Category ?? "General";

                SettingsList.Add(new SettingItem
                {
                    Name = displayName,
                    Description = description,
                    Category = category,
                    PropertyType = prop.PropertyType,
                    Value = prop.GetValue(SelectedMode),
                    Setter = newValue => prop.SetValue(SelectedMode, newValue) // For two-way updates
                });
            }
        }

        // SettingsControl.xaml.cs
        private void IntTemplate_TextBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return; // nothing to do

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;

            // pick the first file and set it into the current SettingItem
            var settingItem = (SettingItem)((FrameworkElement)sender).DataContext;
            settingItem.Value = files[0];     // will trigger the two‑way binding
        }

        // In SettingsControl.xaml.cs
        private void IntTemplate_TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;  // This is critical!
        }

        //private void IntTemplate_TextBox_Drop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //    {
        //        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        //        if (sender is TextBox textBox && files?.Length > 0)
        //        {
        //            textBox.Text = files[0];
        //        }
        //    }
        //    e.Handled = true;
        //}

    }
}
