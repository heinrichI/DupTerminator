using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.WPF.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private object _currentSettings; // The active mode's settings object
        public object CurrentSettings
        {
            get => _currentSettings;
            set
            {
                _currentSettings = value;
                LoadSettings(); // Regenerate the UI when mode changes
                OnPropertyChanged(nameof(CurrentSettings));
            }
        }

        private ObservableCollection<SettingItem> _settingsList = new ObservableCollection<SettingItem>();
        public ObservableCollection<SettingItem> SettingsList
        {
            get => _settingsList;
            set { _settingsList = value; OnPropertyChanged(nameof(SettingsList)); }
        }

        // Load properties via reflection
        private void LoadSettings()
        {
            SettingsList.Clear();
            if (CurrentSettings == null) return;

            var properties = CurrentSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                    Value = prop.GetValue(CurrentSettings),
                    Setter = newValue => prop.SetValue(CurrentSettings, newValue) // For two-way updates
                });
            }
        }

        // Helper class for each setting (used in the collection)
        public class SettingItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public Type PropertyType { get; set; }
            public object Value { get; set; }
            public Action<object> Setter { get; set; } // Callback to update the original property
        }
    }
}
