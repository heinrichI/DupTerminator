using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Settings
{
    public class Mode1Settings : SettingsBase
    {
        private bool _isEnabled;
        private string _userName;
        private int _timeoutSeconds;

        [DisplayName("Enable Feature")] // Custom display name
        [Description("Turns on the main feature.")]
        [Category("General")] // Optional: Group settings
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        [DisplayName("User Name")]
        [Description("Your display name.")]
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(nameof(UserName)); }
        }

        [DisplayName("Timeout (seconds)")]
        [Description("How long to wait before timeout.")]
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set { _timeoutSeconds = value; OnPropertyChanged(nameof(TimeoutSeconds)); }
        }
    }
}
