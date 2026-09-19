using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic
{
    public abstract class SearcherBase<T> where T : ExtendedFileInfo
    {
        private readonly IWindowsUtil _windowsUtil;

        protected SearcherBase(IWindowsUtil windowsUtil)
        {
            _windowsUtil = windowsUtil;
        }

        protected KeyValuePair<string, List<SearchPath>>[] GetPhisicalDrives(ReadOnlyCollection<SearchPath> locations)
        {
            List<string>? driveLetters = new List<string>();
            if (locations != null)
                driveLetters.AddRange(locations.Select(p => p.DriveLetter).Distinct());
            var groupByLetter = locations.GroupBy(l => l.DriveLetter);

            Dictionary<string, List<SearchPath>> dict = new Dictionary<string, List<SearchPath>>();
            foreach (var group in groupByLetter)
            {
                var model = _windowsUtil.GetModelFromDrive(group.Key);
                if (!dict.ContainsKey(model))
                {
                    dict.Add(model, new List<SearchPath>(group));
                }
                else
                {
                    dict[model].AddRange(group);
                }
            }

            return dict.ToArray();
        }

        protected static void CompareBySize(
          ReadOnlyCollection<ExtendedFileInfo> foundedFiles,
          BlockingCollection<ExtendedFileInfo> filesWithEqualSize,
          CancellationToken cancelToken)
        {
            IEnumerable<IGrouping<ulong, ExtendedFileInfo>>? groupsFilesWithEqualSize = foundedFiles.GroupBy(fi => fi.Size).Where(group => group.Count() > 1);
            foreach (IGrouping<ulong, ExtendedFileInfo>? items in groupsFilesWithEqualSize)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("CompareBySize was canceled.");
                    break;
                }

                foreach (ExtendedFileInfo item in items)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("CompareBySize was canceled.");
                        break;
                    }

                    filesWithEqualSize.Add(item);
                }
            }
            filesWithEqualSize.CompleteAdding();
        }
    }
}
