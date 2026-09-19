using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class PHashContainerSettings : PHashSettings
    {
        public override string Name { get; } = "PHash Container";

        private int _moreThanFileCount = 1;

        [DisplayName("MoreThanFileCount")]
        [Description("How long to wait before timeout.")]
        public int MoreThanFileCount
        {
            get => _moreThanFileCount;
            set { _moreThanFileCount = value; RaisePropertyChangedEvent(); }
        }
    }
}
