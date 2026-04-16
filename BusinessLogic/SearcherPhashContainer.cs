using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;


namespace DupTerminator.BusinessLogic
{
    public class SearcherPhashContainer : SearcherPhashBase, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _includeLocations;
        private readonly ReadOnlyCollection<SearchPath> _excludeLocations;
        private readonly PHashContainerSettings _pHashContainerSettings;
        private readonly Stopwatch _stopwatch = new();
        public SearcherPhashContainer(
            ReadOnlyCollection<SearchPath> includeLocations,
            ReadOnlyCollection<SearchPath> excludeLocations,
            SearchSetting searchSetting,
            PHashContainerSettings pHashContainerSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<SearcherMD5> logger) : base(searchSetting, pHashContainerSettings, mIHFactory, pHashService, phashRepository, archiveService, pdfService, windowsUtil, logger)
        {
            _includeLocations = includeLocations;
            _excludeLocations = excludeLocations;
            _pHashContainerSettings = pHashContainerSettings;
        }

        public async Task<Collection<DuplicateContainer>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            _stopwatch.Restart();

            List<PHashDuplicateGroup> duplicateGroups = await GetDuplicateGroupAsync(_includeLocations, _excludeLocations, progress, cancelToken);

            _stopwatch.Stop();
            _logger.LogInformation($"ElapsedTime: {_stopwatch.Elapsed}");

            if (duplicateGroups is null)
                return null;

            Dictionary<ContainerPairKey, (SortedSet<ExtendedFileInfo>, SortedSet<ExtendedFileInfo>)> containers =
                new Dictionary<ContainerPairKey, (SortedSet<ExtendedFileInfo>, SortedSet<ExtendedFileInfo>)>();
            foreach (var group in duplicateGroups)
            {
                if (group.Count > 1)
                {
                    var files = group.ToList();

                    // Iterate through all pairs (i, j) where i < j
                    for (int i = 0; i < files.Count; i++)
                    {
                        for (int j = i + 1; j < files.Count; j++)
                        {
                            var first = files[i];
                            var second = files[j];

                            if (first.FileItem.FileInfo.Container.Equals(second.FileItem.FileInfo.Container))
                                continue;

                            // Ensure the pair is ordered by path (to avoid duplicates)
                            if (string.Compare(first.FileItem.FileInfo.Container.Path, second.FileItem.FileInfo.Container.Path, StringComparison.Ordinal) > 0)
                            {
                                // Swap if first.Path > second.Path
                                (first, second) = (second, first);
                            }


                            var key = new ContainerPairKey(first.FileItem.FileInfo.Container, second.FileItem.FileInfo.Container, first.FileItem.FileInfo, second.FileItem.FileInfo);

                            // Initialize the list if the key doesn't exist
                            if (!containers.TryGetValue(key, out (SortedSet<ExtendedFileInfo>, SortedSet<ExtendedFileInfo>) value))
                            {
                                value = (new SortedSet<ExtendedFileInfo>(), new SortedSet<ExtendedFileInfo>());
                                containers[key] = value;
                            }

                            // Add the current checksum's file list (or just the checksum? Clarify your intent)
                            // Since the value is List<ExtendedFileInfo>, you might want to add all files from the current checksum group?
                            // Alternatively, if you want to aggregate which checksum groups contribute to this container pair,
                            // you might add the entire list (pair.Value) or just the current file pair?
                            // Based on your structure, it seems you want to collect all ExtendedFileInfo from checksum groups that have this container pair.
                            // But note: this might add duplicates if the same file appears in multiple checksum groups?
                            // Clarification needed: What exactly should be in the List<ExtendedFileInfo> value?

                            // Assuming you want to add all files from the current checksum group (pair.Value)
                            //value.AddRange([first, second]);
                            //if (!value.Item1.Contains(first.FileItem.FileInfo))
                            value.Item1.Add(first.FileItem.FileInfo);
                            //if (!value.Item2.Contains(second.FileItem.FileInfo))
                            value.Item2.Add(second.FileItem.FileInfo);
                        }
                    }
                }
            }



            var cts = containers
                .Where(c => c.Value.Item1.Count > _pHashContainerSettings.MoreThanFileCount)
                .Select(c => new DuplicateContainer(c))
                .OrderByDescending(d => d.SizeOfEqualFiles)
                .ToList();

            return new Collection<DuplicateContainer>(cts);
        }



        public void Dispose()
        {
            //_cts?.Dispose();
        }

        //public void Cancell()
        //{
        //    // Token can only be canceled once.
        //    _cts.Cancel();
        //}
    }
}
