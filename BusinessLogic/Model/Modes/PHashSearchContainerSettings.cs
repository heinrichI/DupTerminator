using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class PHashSearchContainerSettings : SettingsBase
    {
        public override string Name { get; } = "PHash Search Container";

        private int _hammingDistance = 1;

        [DisplayName("Hamming Distance")] // Custom display name
        [Description("Turns on the main feature.")]
        [Category("General")] // Optional: Group settings
        public int HammingDistance
        {
            get => _hammingDistance;
            set { _hammingDistance = value; RaisePropertyChangedEvent(); }
        }

        private int _wordLength = 16;

        [DisplayName("WordLength")] // Custom display name
        [Description("Turns on the main feature.")]
        [Category("General")] // Optional: Group settings
        public int WordLength
        {
            get => _wordLength;
            set { _wordLength = value; RaisePropertyChangedEvent(); }
        }

        private int _moreThanFileCount = 1;

        [DisplayName("MoreThanFileCount")]
        [Description("How long to wait before timeout.")]
        public int MoreThanFileCount
        {
            get => _moreThanFileCount;
            set { _moreThanFileCount = value; RaisePropertyChangedEvent(); }
        }

        [DisplayName("Target")]
        [Description("Target.")]
        public string Target { get; set; }
    }
}
