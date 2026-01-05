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
    public class SearcherPhashSearchContainer : SearcherPhashBase, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly PHashSearchContainerSettings _pHashSearchContainerSettings;
        private readonly IMIHFactory _mIHFactory;
        private readonly Stopwatch _stopwatch = new();
        public SearcherPhashSearchContainer(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            PHashSearchContainerSettings pHashSearchContainerSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<Searcher> logger) : base(searchSetting, new PHashSettings(), mIHFactory, pHashService, phashRepository, archiveService, pdfService, windowsUtil, logger)
        {
            _locations = locations;
            _pHashSearchContainerSettings = pHashSearchContainerSettings;
            _mIHFactory = mIHFactory;
        }

        public async Task<ReadOnlyCollection<DuplicateContainer>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            var finalResultBag = new ConcurrentBag<(ArchiveFileInfo efi, ulong phash, int width, int height)>();
            ExtendedFileInfo? targetEfi = null;

            if (Directory.Exists(_pHashSearchContainerSettings.Target))
            {
                DirectoryInfo di = new System.IO.DirectoryInfo(_pHashSearchContainerSettings.Target);
                var dFiles = di.GetFiles();
                var files3 = dFiles.Select(f => new ExtendedFileInfo()
                {
                    Size = Convert.ToUInt64(f.Length),
                    Name = f.Name,
                    Path = f.FullName,
                    //LastAccessTime = f.LastAccessTime,
                    LastWriteTime = f.LastWriteTime,
                    DirectoryName = f.DirectoryName,
                    Extension = f.Extension,
                    ContainerFilesCount = dFiles.Length
                });
                foreach (var item in files3)
                {
                }
            }
            else if (_archiveService.IsArchiveFile(_pHashSearchContainerSettings.Target))
            {
                var fi = new FileInfo(_pHashSearchContainerSettings.Target);
                if (fi.Exists)
                {
                    targetEfi = new ExtendedFileInfo()
                    {
                        Size = Convert.ToUInt64(fi.Length),
                        Name = fi.Name,
                        Path = fi.FullName,
                        //LastAccessTime = fi.LastAccessTime,
                        LastWriteTime = fi.LastWriteTime,
                        DirectoryName = fi.DirectoryName,
                        Extension = fi.Extension,
                    };

                    var streamPairs = _archiveService.GetStreams(targetEfi, _pHashService.IsSupportedExtension, cancelToken);
                    targetEfi.ContainerFilesCount = streamPairs.Count;

                    Parallel.ForEach(
                       streamPairs,
                       new ParallelOptions { MaxDegreeOfParallelism = _numberOfConsumers },
                       // localInit
                       () => new List<(ArchiveFileInfo efi, ulong phash, int width, int height)>(), // localInit: Initialize a new local list for each task/partition
                                                                                                    // body
                       (item, loopState, localList) => // body: The loop body logic
                       {
                           using (item.Item2)
                           {
                               try
                               {
                                   (ulong? phash, int width, int height) result = _pHashService.CalculatePHash(item.Item2);
                                   if (result.phash.HasValue)
                                   {
                                       localList.Add((item.Item1, result.phash.Value, result.width, result.height));
                                   }
                                   else
                                   {
                                       _logger.LogWarning($"phash empty for {item.Item1.Path}");
                                   }
                               }
                               catch (Exception ex)
                               {
                                   _logger.LogError(ex.Message, ex);
                               }
                               return localList; // Return the updated local list for the next iteration
                           }
                       },
                       (finalLocalList) => // localFinally: Action to combine results
                       {
                           foreach ((ArchiveFileInfo efi, ulong phash, int width, int height) item2 in finalLocalList)
                           {
                               Debug.Assert(item2.phash != 0);
                               finalResultBag.Add(item2);
                           }
                       }
                    );
                }

            }

            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = await CalculateChecksum(_locations, progress, cancelToken);

            if (checksumDictionary.Any())
            {
                List<DuplicateContainer>? resultList = new List<DuplicateContainer>();
                using (var mih = _mIHFactory.Create())
                {
                    progress?.Report(new ProgressDto
                    {
                        State = "Start train MIH",
                        RemainSize = string.Empty,
                    });

                    mih.Update(checksumDictionary);
                    mih.Train(wordLength: _pHashSearchContainerSettings.WordLength, threshold: _pHashSearchContainerSettings.HammingDistance);

                    progress?.Report(new ProgressDto
                    {
                        State = "Ended train MIH",
                        RemainSize = string.Empty,
                    });

                    Dictionary<string, ContainerInfo> containers = new Dictionary<string, ContainerInfo>();
                    foreach (var targetItem in finalResultBag)
                    {
                        Debug.Assert(targetItem.phash != 0);

                        (ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(targetItem.phash).ToArray();

                        foreach ((ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance) queryItem in resultQuery)
                        {
                            foreach (var fileItem in queryItem.FileInfos)
                            {
                                if (fileItem.FileInfo.Container.Path != _pHashSearchContainerSettings.Target)
                                {
                                    ContainerInfo container;
                                    if (!containers.ContainsKey(fileItem.FileInfo.Container.Path))
                                    {
                                        container = new ContainerInfo(fileItem.FileInfo.Container);
                                        container.Info.ContainerFilesCount = fileItem.FileInfo.ContainerFilesCount;
                                        containers.Add(fileItem.FileInfo.Container.Path, container);
                                    }
                                    else
                                    {
                                        container = containers[fileItem.FileInfo.Container.Path];
                                    }
                                    container.FirstFiles.Add(targetItem.efi);
                                    container.SecondFiles.Add(fileItem.FileInfo);
                                }
                            }
                        }
                    }

                    var cts = containers
                        .Where(c => c.Value.FirstFiles.Count > _pHashSearchContainerSettings.MoreThanFileCount)
                        .Select(c => new DuplicateContainer(targetEfi, c.Value.Info, c.Value.FirstFiles, c.Value.SecondFiles))
                        .OrderByDescending(d => d.SizeOfEqualFiles)
                        .ToList();

                    return new ReadOnlyCollection<DuplicateContainer>(cts);
                }
                //resultList.Sort((x, y) => x.HammingDistance.CompareTo(y.HammingDistance));
                return new ReadOnlyCollection<DuplicateContainer>(resultList);
            }

            return null;
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

        class ContainerInfo()
        {
            public List<ExtendedFileInfo> FirstFiles { get; } = new List<ExtendedFileInfo>();

            public List<ExtendedFileInfo> SecondFiles { get; } = new List<ExtendedFileInfo>();

            public ContainerInfo(ExtendedFileInfo container) : this()
            {
                Info = container;
            }

            public ExtendedFileInfo Info { get; }
        }
    }
}
