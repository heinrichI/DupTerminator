using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.ViewModel
{
    public class ProgressItemViewModel : PropertyChangedBase
    {
        public ProgressItemViewModel(ProgressDto dto)
        {
            PhisicalDrive = dto.PhisicalDrive;
            Path = dto.Path;
            State = dto.State;
            RemainSize = dto.RemainSize;
        }

        private string _phisicalDrive;
        public string PhisicalDrive
        {
            get => _phisicalDrive;
            set
            {
                _phisicalDrive = value;
                RaisePropertyChangedEvent();
            }
        }


        private string _path = string.Empty;
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                RaisePropertyChangedEvent();
            }
        }

        private string _state = string.Empty;
        public string State
        {
            get => _state;
            set
            {
                _state = value;
                RaisePropertyChangedEvent();
            }
        }

        private string _remainSize;
        public string RemainSize
        {
            get => _remainSize;
            set
            {
                _remainSize = value;
                RaisePropertyChangedEvent();
            }
        }
    }
}
