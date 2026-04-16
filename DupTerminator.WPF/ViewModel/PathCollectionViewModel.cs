using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.WPF.Helper;

namespace DupTerminator.WPF.ViewModel
{
    public class PathCollectionViewModel : PropertyChangedBase, IDropable
    {
        public ObservableCollection<SearchPathViewModel> Locations { get; set; } = new ObservableCollection<SearchPathViewModel>();

        #region IDropable
        void IDropable.Drop(object dropData)
        {
            if (dropData is string[] filepaths)
            {
                foreach (string path in filepaths)
                {
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (IOHelper.IsDirectory(path))
                    {
                        Locations.Add(new SearchPathViewModel
                        {
                            Path = path,
                            IsDirectory = true,
                            SearchInSubfolder = true
                        });
                    }
                    else if (File.Exists(path))
                    {
                        Locations.Add(new SearchPathViewModel
                        {
                            Path = path,
                            IsDirectory = false
                        });
                    }
                }
            }
        }
        #endregion
    }
}
