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
using DupTerminator.BusinessLogic.Model.Settings;
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
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSettings();
        }

       

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
    }
}
