using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class PHashSettings : SettingsBase
    {
        private int _hammingDistance = 7;

        [DisplayName("Hamming Distance")] // Custom display name
        [Description("Turns on the main feature.")]
        [Category("General")] // Optional: Group settings
        public int HammingDistance
        {
            get => _hammingDistance;
            set { _hammingDistance = value; OnPropertyChanged(nameof(HammingDistance)); }
        }

        private int _wordLength = 16;

        [DisplayName("WordLength")] // Custom display name
        [Description("Turns on the main feature.")]
        [Category("General")] // Optional: Group settings
        public int WordLength
        {
            get => _wordLength;
            set { _wordLength = value; OnPropertyChanged(nameof(WordLength)); }
        }

        [DisplayName("CheckAllFilesInGroup")] // Custom display name
        [Description("CheckAllFilesInGroup.")]
        public bool CheckAllFilesInGroup { get; set; } = false;
    }
}
