using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.WPF.Helper;

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
        public class SettingItem : PropertyChangedBase, IDropable
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public Type PropertyType { get; set; }
            //public object Value { get; set; }
            private object _value;
            public object Value
            {
                get => _value;
                set
                {
                    // Only update if value changed
                    if (Equals(_value, value)) return;

                    _value = value;
                    RaisePropertyChangedEvent();

                    // Propagate change to the SelectedMode property
                    Setter?.Invoke(ConvertValue(value, PropertyType));
                }
            }

            // Handle type conversion (string → int, bool, etc.)
            private object ConvertValue(object value, Type targetType)
            {
                if (value != null && targetType.IsAssignableFrom(value.GetType()))
                    return value;

                try
                {
                    return Convert.ChangeType(value, targetType);
                }
                catch
                {
                    return value; // Fallback (may require validation)
                }
            }

            public Action<object> Setter { get; set; } // Callback to update the original property

            #region IDropable Members

            void IDropable.Drop(object dropData)
            {
                var filepaths = dropData as string[];
                if (filepaths != null && filepaths.Any())
                {
                    Value = filepaths[0];
                    //foreach (string path in filepaths)
                    //{
                    //    if (!String.IsNullOrEmpty(path))
                    //    {
                    //        if (IOHelper.IsDirectory(path))
                    //        {
                    //            _locationsObservable.Add(new SearchPathViewModel
                    //            {
                    //                Path = path,
                    //                IsDirectory = true,
                    //                SearchInSubfolder = true
                    //                //Image = IconReader.GetIcon(path, true);
                    //            });
                    //        }
                    //        else if (System.IO.File.Exists(path))
                    //        {
                    //            _locationsObservable.Add(new SearchPathViewModel
                    //            {
                    //                Path = path,
                    //                IsDirectory = false
                    //            });
                    //        }
                    //    }
                    //}
                }
            }

            #endregion
        }
    }
}
