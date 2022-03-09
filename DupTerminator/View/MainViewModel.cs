using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.View
{
    enum SeacrhState
    {
        ShowDuplicate,
        Search,
        Pause
    }

    internal class MainViewModel : BasePropertyChanged
    {
        public MainViewModel()
        {
            PathOfSearch = new List<DuplicateDirectory>();
            PathOfSkip = new List<DuplicateDirectory>();
        }

        public List<DuplicateDirectory> PathOfSearch { get; set; }
        public List<DuplicateDirectory> PathOfSkip { get; set; }

        private SeacrhState _seacrhState = SeacrhState.ShowDuplicate;
        public SeacrhState SeacrhState
        {
            get { return _seacrhState; }
            set 
            { 
                _seacrhState = value; 
                RaisePropertyChangedEvent(); 
            }
        }

        IList<FileViewModel>? _dublicates;
        public IList<FileViewModel>? Dublicates
        {
            get { return _dublicates; }
            set 
            {
                _dublicates = value; 
                RaisePropertyChangedEvent();
            }
        }

    }
}
