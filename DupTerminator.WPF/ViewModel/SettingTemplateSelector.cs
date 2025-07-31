using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static DupTerminator.WPF.ViewModel.SettingsViewModel;

namespace DupTerminator.WPF.ViewModel
{
    public class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate IntTemplate { get; set; }
        // Add more templates for other types (e.g., DateTime, Enum)

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SettingItem setting)
            {
                if (setting.PropertyType == typeof(bool)) return BoolTemplate;
                if (setting.PropertyType == typeof(string)) return StringTemplate;
                if (setting.PropertyType == typeof(int)) return IntTemplate;
                // Extend for more types
            }
            return null;
        }
    }
}
