using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Model
{
    internal class SettingsSerializable
    {
        public SearchPathViewModel[] Locations { get; set; } = new SearchPathViewModel[0];
        public SearchSetting SearchSetting { get; set; } = new SearchSetting();
        public string SelectedMode { get; set; }
    }
}
