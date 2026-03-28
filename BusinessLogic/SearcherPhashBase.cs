using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;

namespace DupTerminator.BusinessLogic
{
    public class SearcherPhashBase
    {
        protected readonly int _numberOfConsumers = Environment.ProcessorCount;
        protected readonly SearchSetting _searchSetting;
        protected readonly PHashSettings _pHashSettings;
        private readonly IMIHFactory _mihFactory;
        protected readonly IPHashService _pHashService;
        private readonly IPhashRepository _phashRepository;
        protected readonly IArchiveService _archiveService;
        private readonly IPdfService _pdfService;
        private readonly IWindowsUtil _windowsUtil;
        protected readonly ILogger _logger;

        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);

        public SearcherPhashBase(
            SearchSetting searchSetting,
            PHashSettings pHashSettings,
            IMIHFactory mihFactory,
            IPHashService pHashService,
            IPhashRepository phashRepository,
            IArchiveService archiveService,
            IPdfService pdfService,
            IWindowsUtil windowsUtil,
            ILogger logger)
        {
            _searchSetting = searchSetting;
            _pHashSettings = pHashSettings;
            _mihFactory = mihFactory;
            _pHashService = pHashService;
            _phashRepository = phashRepository;
            _archiveService = archiveService;
            _pdfService = pdfService;
            _windowsUtil = windowsUtil;
            _logger = logger;
        }

        protected async Task<List<PHashDuplicateGroup>> GetDuplicateGroupAsync(
            ReadOnlyCollection<SearchPath> locations,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = await CalculateChecksum(locations, progress, cancelToken);

            //Debug.Assert(_checksumDictionary.Count > 0, "Файлов нет!");
            //var paths = checksumDictionary.SelectMany(f => f.Value.Select(h => h.FileInfo.Path)).ToArray();
            //foreach (var path in paths)
            //{
            //    if (paths.Count(f => f == path) > 1)
            //        throw new Exception("Что-то не так");
            //}

            if (cancelToken.IsCancellationRequested)
                return null;

            if (checksumDictionary.Any())
            {
                Dictionary<ExtendedFileInfo, int> groupIndexByInfo = new Dictionary<ExtendedFileInfo, int>();
                HashSet<ulong> skip = new HashSet<ulong>();
                List<PHashDuplicateGroup> duplicateGroups = new List<PHashDuplicateGroup>();
                using (var mih = _mihFactory.Create())
                {
                    progress?.Report(new ProgressDto
                    {
                        State = $"Start train MIH for {checksumDictionary.Count} hashes",
                        RemainSize = string.Empty,
                    });

                    mih.Update(checksumDictionary);
                    mih.Train(wordLength: _pHashSettings.WordLength, threshold: _pHashSettings.HammingDistance);

                    progress?.Report(new ProgressDto
                    {
                        State = "Ended train MIH",
                        RemainSize = string.Empty,
                    });


                    int total = checksumDictionary.Count;
                    int index = 0;
                    foreach (var pair in checksumDictionary)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("MIH quering was canceled.");
                            break;
                        }

                        if (skip.Contains(pair.Key))
                            continue;

                        progress?.Report(new ProgressDto
                        {
                            Path = pair.Value.First().FileInfo.Path,
                            State = "QueryMIH",
                            RemainSize = $"{index }/{total}",
                        });
                        index++;

                        PHashDuplicateGroup duplicateGroup = null;
                        //if (pair.Value.Count > 1)
                        //{
                        duplicateGroup = GetDuplicateGroup(groupIndexByInfo, duplicateGroups, pair.Value.First().FileInfo);
                        foreach (var file in pair.Value)
                        {
                            if (!duplicateGroup.ContainsPath(file.FileInfo.Path))
                            {
                                PHashFileInfoSearchItem? fileInfo = new PHashFileInfoSearchItem(file, PHashFileInfoSearchItem.SearchType.Seed);
                                duplicateGroup.Add(fileInfo);
                            }
                        }


                        (ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(pair.Key).ToArray();
                        skip.Add(pair.Key);
                        if (resultQuery.Length <= 1)
                            continue;
                        foreach ((ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance) queryItem in resultQuery)
                        {
                            if (queryItem.Hash == pair.Key)
                                continue;
                            if (!_pHashSettings.CheckAllFilesInGroup)
                            {
                                skip.Add(queryItem.Hash);
                            }
                            foreach (var fileItem in queryItem.FileInfos)
                            {
                                //if (fileItem == pair.Value.First())
                                if (duplicateGroup.ContainsPath(fileItem.FileInfo.Path))
                                    continue;
                                PHashFileInfoSearchItem isi;
                                if (queryItem.Hash == pair.Key)
                                {
                                    Debug.Assert(queryItem.HammingDistance == 0);
                                    isi = new PHashFileInfoSearchItem(fileItem, PHashFileInfoSearchItem.SearchType.Seed);
                                }
                                else
                                    isi = new PHashFileInfoSearchItem(fileItem, queryItem.HammingDistance);

                                //if (duplicateGroup is null)
                                //    duplicateGroup = GetDuplicateGroup(groupIndexByInfo, duplicateGroups, fileItem);
                                duplicateGroup.Add(isi);
                                groupIndexByInfo[fileItem.FileInfo] = duplicateGroups.IndexOf(duplicateGroup);
                            }
                        }
                    }
                }
                return duplicateGroups;
            }

            return null;

            static PHashDuplicateGroup GetDuplicateGroup(Dictionary<ExtendedFileInfo, int> groupIndexByInfo, List<PHashDuplicateGroup> duplicateGroups, ExtendedFileInfo fileItem)
            {
                PHashDuplicateGroup duplicateGroup;
                if (!groupIndexByInfo.ContainsKey(fileItem))
                { // new group
                    duplicateGroup = new PHashDuplicateGroup();
                    duplicateGroups.Add(duplicateGroup);
                    groupIndexByInfo[fileItem] = duplicateGroups.IndexOf(duplicateGroup);
                }
                else
                {
                    duplicateGroup = duplicateGroups[groupIndexByInfo[fileItem]];
                }

                return duplicateGroup;
            }
        }

        protected async Task<ConcurrentDictionary<ulong, IList<PHashFileInfo>>> CalculateChecksum(
            ReadOnlyCollection<SearchPath> locations,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = new ConcurrentDictionary<ulong, IList<PHashFileInfo>>();

            KeyValuePair<string, List<SearchPath>>[] phisicalDrives = GetPhisicalDrives(locations);

            //(BlockingCollection<ExtendedFileInfo> BlockingCollection, string Drive)[] blockingCollectionByPhisDisks =
            //    new (BlockingCollection<ExtendedFileInfo>, string drive)[phisicalDrives.Length];
            //for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            //{
            //    blockingCollectionByPhisDisks[i] = new(
            //        new BlockingCollection<ExtendedFileInfo>(), phisicalDrives[i].Key);
            //}

            var tasksSearch = new Task<ReadOnlyCollection<ExtendedFileInfo>>[phisicalDrives.Length];
            for (int i = 0; i < phisicalDrives.Length; i++)
            {
                int temp = i;
                tasksSearch[i] = Task.Run(() => SearchFileOnPhisicalDrive(progress, phisicalDrives[temp].Key, phisicalDrives[temp].Value, cancelToken))
               .ContinueWith(t =>
               {
                   // Force progress report after task completion, even if failed
                   progress?.Report(new ProgressDto
                   {
                       PhisicalDrive = phisicalDrives[temp].Key,
                       State = "Search ended",
                   });
                   return t.Result; // Return the actual result
               }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            ReadOnlyCollection<ExtendedFileInfo>[]? result = await Task.WhenAll(tasksSearch);
            Task[] tasksByDrivers = new Task[phisicalDrives.Length];


            BlockingCollection<(ExtendedFileInfo, Stream)> blockingCollection = new(1000);

            for (int i = 0; i < phisicalDrives.Length; i++)
            {
                int temp = i;
                tasksByDrivers[temp] = Task.Run(() => FillStreamBuffer(
                        result[temp],
                        phisicalDrives[temp].Key,
                        blockingCollection,
                        checksumDictionary,
                        progress,
                        cancelToken))
                     .ContinueWith(t => progress?.Report(new ProgressDto
                     {
                         PhisicalDrive = phisicalDrives[temp].Key,
                         State = "FillStreamBuffer ended",
                         RemainSize = string.Empty,
                     }), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            // Wait for both producer and all consumers to complete
            _ = Task.WhenAll(tasksByDrivers).ContinueWith(t => blockingCollection.CompleteAdding(), TaskContinuationOptions.ExecuteSynchronously);


            Task[] consumers = new Task[_numberOfConsumers];
            for (int i = 0; i < _numberOfConsumers; i++)
            {
                int consumerId = i;
                consumers[i] = Task.Run(() => // Task.Run is preferred over Task.Factory.StartNew
                {
                    CalculateCheckSum(blockingCollection, checksumDictionary, progress, cancelToken);
                });
            }

            await Task.WhenAll(Task.WhenAll(tasksByDrivers), Task.WhenAll(consumers));
            blockingCollection = null;

            //var sr = await Task.WhenAll(tasksByDrivers).ContinueWith((tasks2) =>
            //{
            //    foreach (var item in blockingCollectionByPhisDisks)
            //    {
            //        item.BlockingCollection.Dispose();
            //    }
            //    IEnumerable<DuplicateGroup>? duplicates = _checksumDictionary
            //        .Where(pair => pair.Value.Count > 1)
            //        .Select(pair => new DuplicateGroup(pair.Key, pair.Value));
            //    //.OrderByDescending(d => d.Files.Any(f => f.Container is null));

            //    var withoutContainer = duplicates.SelectMany(f => f.Files).Where(d => d.Container is null);
            //    var d2 = duplicates.Where(d => d.Files.Any(f => withoutContainer.Any(c => f.Container is not null && c.Path == f.Container.Path)));
            //    if (d2 != null && d2.Any())
            //    {
            //        _logger.LogInformation($"Контейнеров с дублями: {d2.Count()}");
            //    }

            //    return new ReadOnlyCollection<DuplicateGroup>(duplicates.Except(d2).ToList());
            //});


            //await Task.WhenAll(tasksByDrivers);

            return checksumDictionary;
        }

        private KeyValuePair<string, List<SearchPath>>[] GetPhisicalDrives(ReadOnlyCollection<SearchPath> locations)
        {
            List<string>? driveLetters = new List<string>();
            if (locations != null)
                driveLetters.AddRange(locations.Select(p => p.DriveLetter).Distinct());
            var groupByLetter = locations.GroupBy(l => l.DriveLetter);

            Dictionary<string, List<SearchPath>> dict = new Dictionary<string, List<SearchPath>>();
            foreach (var group in groupByLetter)
            {
                var model = _windowsUtil.GetModelFromDrive(group.Key);
                if (model is null)
                    continue;
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

        private void CalculateCheckSum(
            BlockingCollection<(ExtendedFileInfo, Stream)> blockingCollection,
            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            foreach (var item in blockingCollection.GetConsumingEnumerable())
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["Path"] = item.Item1.Path
                }))

                if (cancelToken.IsCancellationRequested)
                {
                    _logger.LogInformation("MIH quering was canceled.");
                    break;
                }

                progress?.Report(new ProgressDto
                {
                    Path = item.Item1.Path,
                    State = "CalculateCheckSum",
                    RemainSize = string.Empty,
                });

                using (item.Item2)
                {
                    var result = _pHashService.CalculatePHash(item.Item2);

                    if (!result.phash.HasValue)
                        continue;
                    Debug.Assert(result.phash != 0);

                    if (_searchSetting.UseDB)
                    {
                        var lastWriteTime = GetLastWriteTime(item);
                        Task.Run(() => _phashRepository.Add(item.Item1.Path, lastWriteTime, item.Item1.Size, result.phash.Value, result.width, result.height));
                        _logger.LogDebug($"Add {item.Item1.Path} - {lastWriteTime} - {item.Item1.Size}: {result.phash}");
                    }
                    checksumDictionary.AddOrUpdate(result.phash.Value,
                        addValueFactory: (checksum) =>
                        {
                            var list = new List<PHashFileInfo>();
                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.Item1, checksum, result.width, result.height);
                            list.Add(pHashFileInfo);
                            return list;
                        },
                        updateValueFactory: (checksum, list) =>
                        {
                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.Item1, checksum, result.width, result.height);
                            list.Add(pHashFileInfo);
                            return list;
                        });
                }
            }
        }

        private static DateTime GetLastWriteTime((ExtendedFileInfo, Stream) item)
        {
            if (item.Item1 is ArchiveFileInfo afi)
            {

                if (afi.ArchiveInArchive)
                {
                    if (afi.Container.Container != null && afi.Container.Container.LastWriteTime != DateTime.MinValue)
                        return afi.Container.Container.LastWriteTime;
                    else
                    {
                        return afi.Container.Container.Container.LastWriteTime;
                    }
                }
                else
                    return item.Item1.Container.LastWriteTime;
            }
            return item.Item1.LastWriteTime;
        }

        // из разных потоков
        private ReadOnlyCollection<ExtendedFileInfo> SearchFileOnPhisicalDrive(
            IProgress<ProgressDto> progress,
            in string phisicalDrive,
            IEnumerable<SearchPath> locations,
            CancellationToken token)
        {
            List<ExtendedFileInfo> files = new List<ExtendedFileInfo>();
            foreach (var directory in locations.Where(p => p.IsDirectory))
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogInformation("SearchFileOnPhisicalDrive was canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = directory.Path, State = "Search" });

                if (!Directory.Exists(directory.Path))
                    throw new Exception("Directory does not exists!");
                DirectoryInfo di = new System.IO.DirectoryInfo(directory.Path);
                AddFilesFromDirectory(di, ref files, directory.SearchInSubFolder, token, progress, phisicalDrive);
            }
            foreach (var file in locations.Where(p => !p.IsDirectory))
            {
                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = file.Path, State = "Search" });

                AddFile(file, ref files, token, progress, phisicalDrive);
            }

            //progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = string.Empty, State = "Search ended" });

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }

        private void AddFile(SearchPath file, ref List<ExtendedFileInfo> files, CancellationToken token, IProgress<ProgressDto> progress, string phisicalDrive)
        {
            if (token.IsCancellationRequested)
            {
                _logger.LogInformation("AddFiles canceled.");
                return;
            }

            progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = file.Path, State = "Search" });

            var fi = new FileInfo(file.Path);
            var f = new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(fi.Length),
                Name = fi.Name,
                Path = fi.FullName,
                //LastAccessTime = fi.LastAccessTime,
                LastWriteTime = fi.LastWriteTime,
                DirectoryName = fi.DirectoryName,
                Extension = fi.Extension,
            };

            files.Add(f);
            //if (_archiveService.IsArchiveFile(file.Path))
            //{
            //    var filesInArchive = _dbArchiveService.Get(_searchSetting.UseDB, f, token);
            //    foreach (ExtendedFileInfo archFile in filesInArchive)
            //    {
            //        files.Add(archFile);
            //    }
            //}
        }

        private async void FillStreamBuffer(
           ReadOnlyCollection<ExtendedFileInfo> collection,
           string drive,
           BlockingCollection<(ExtendedFileInfo, Stream)> toCalculateCollection,
           ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary,
           IProgress<ProgressDto> progress,
           CancellationToken cancelToken)
        {
            if (_phashRepository is null)
                throw new ArgumentNullException(nameof(_phashRepository));

            decimal totalSize = collection.Sum(b => (decimal)b.Size);
            decimal calculatedSize = 0;
            foreach (var fileInfo in collection)
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["Path"] = fileInfo.Path
                }))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("CalculateCheckSum was canceled.");
                        break;
                    }

                    progress.Report(new ProgressDto
                    {
                        Path = fileInfo.Path,
                        State = "FillStreamBuffer",
                        PhisicalDrive = drive,
                        RemainSize = StringHelper.FormatBytes(totalSize - calculatedSize)
                    });
                    calculatedSize += fileInfo.Size;

                    try
                    {
                        if (_pHashService.IsSupportedExtension(fileInfo.Extension))
                        {
                            if (_searchSetting.UseDB)
                            {
                                var lastWriteTime = fileInfo.LastWriteTime;
                                var result = _phashRepository.Get(fileInfo.Path, lastWriteTime, fileInfo.Size);
                                if (result == null)
                                {
                                    var memoryStream = new ChunkedMemoryStream((int)fileInfo.Size);
                                    using (var fileStream = System.IO.File.OpenRead(fileInfo.Path))
                                    {
                                        // Copy the entire contents of the FileStream into the MemoryStream
                                        fileStream.CopyTo(memoryStream);
                                    }

                                    // Optional: Reset the position to the beginning of the MemoryStream for subsequent read operations
                                    memoryStream.Position = 0;
                                    toCalculateCollection.Add((fileInfo, memoryStream));
                                    //_phashRepository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, phash2, width, height);
                                }
                                else
                                {
                                    Debug.Assert(result.Value.phash != 0);
                                    checksumDictionary.AddOrUpdate(result.Value.phash,
                                        addValueFactory: (checksum) =>
                                        {
                                            var list = new List<PHashFileInfo>();
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(fileInfo, checksum, result.Value.width, result.Value.height);
                                            list.Add(pHashFileInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checksum, list) =>
                                        {
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(fileInfo, checksum, result.Value.width, result.Value.height);
                                            list.Add(pHashFileInfo);
                                            return list;
                                        });
                                }
                            }
                            else
                            {
                                var memoryStream = new ChunkedMemoryStream((int)fileInfo.Size);
                                using (var fileStream = System.IO.File.OpenRead(fileInfo.Path))
                                {
                                    // Copy the entire contents of the FileStream into the MemoryStream
                                    fileStream.CopyTo(memoryStream);
                                }

                                // Optional: Reset the position to the beginning of the MemoryStream for subsequent read operations
                                memoryStream.Position = 0;
                                toCalculateCollection.Add((fileInfo, memoryStream));
                            }
                        }
                        else if (_archiveService.IsArchiveFile(fileInfo.Path))
                        {
                            //слишком частая запись в sql
                            //c базой но без Parallel
                            //DupTerminator.BusinessLogic.Searcher: Information: ElapsedTime: 00:04:52.1189766

                            //var streamPairs = _archiveService.GetStreams(fileInfo, _pHashService.IsSupportedExtension, cancelToken);
                            //foreach (var streamPair in streamPairs)
                            //{
                            //    if (_searchSetting.UseDB)
                            //    {
                            //        var result = _phashRepository.Get(streamPair.Item1.Path, fileInfo.LastWriteTime, streamPair.Item1.Size);
                            //        if ( result != null)
                            //        {
                            //            _checksumDictionary.AddOrUpdate(result.Value.phash,
                            //            addValueFactory: (checksum) =>
                            //            {
                            //                var list = new List<PHashFileInfo>();
                            //                PHashFileInfo pHashFileInfo = new PHashFileInfo(streamPair.Item1);
                            //                pHashFileInfo.Width = result.Value.width;
                            //                pHashFileInfo.Height = result.Value.height;
                            //                list.Add(pHashFileInfo);
                            //                return list;
                            //            },
                            //            updateValueFactory: (checksum, list) =>
                            //            {
                            //                PHashFileInfo pHashFileInfo = new PHashFileInfo(streamPair.Item1);
                            //                pHashFileInfo.Width = result.Value.width;
                            //                pHashFileInfo.Height = result.Value.height;
                            //                list.Add(pHashFileInfo);
                            //                return list;
                            //            });
                            //        }
                            //        else
                            //        {
                            //            toCalculateCollection.Add(streamPair);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        toCalculateCollection.Add(streamPair);
                            //    }                            
                            //}


                            if (_searchSetting.UseDB)
                            {
                                //общий blocking
                                //DupTerminator.BusinessLogic.Searcher: Information: ElapsedTime: 00:03:12.7702661
                                //13gb ram

                                //blocking 1000
                                //DupTerminator.BusinessLogic.Searcher: Information: ElapsedTime: 00:03:18.9046361
                                //6.6gb ram

                                //Parralel
                                //DupTerminator.BusinessLogic.Searcher: Information: ElapsedTime: 00:04:59.4542301
                                //5.9gb ram
                                var dbCollection = _phashRepository.GetContainerHashes(fileInfo);
                                if (dbCollection == null)
                                {
                                    var streamPairs = _archiveService.GetStreams(fileInfo, _pHashService.IsSupportedExtension, cancelToken);

                                    var finalResultBag = new ConcurrentBag<(ExtendedFileInfo efi, ulong phash, int width, int height)>();

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
                                               (ulong? phash, int width, int height) result = _pHashService.CalculatePHash(item.Item2);
                                               if (result.phash.HasValue)
                                               {
                                                   localList.Add((item.Item1, result.phash.Value, result.width, result.height));
                                               }
                                               else
                                               {
                                                   _logger.LogWarning($"phash empty for {item.Item1.Path}");
                                               }
                                               return localList; // Return the updated local list for the next iteration
                                           }
                                       },
                                       (finalLocalList) => // localFinally: Action to combine results
                                       {
                                           foreach (var item in finalLocalList)
                                           {
                                               //Debug.Assert(item.phash != 0);
                                               Debug.Assert(item.width != 0);
                                               Debug.Assert(item.height != 0);
                                               finalResultBag.Add(item);
                                           }
                                       }
                                    );

                                    if (!cancelToken.IsCancellationRequested)
                                    {
                                        _phashRepository.AddContainerStreams(fileInfo, finalResultBag.ToArray());
                                    }
                                    foreach (var item in finalResultBag)
                                    {
                                        //Debug.Assert(item.phash != 0);
                                        Debug.Assert(item.width != 0);
                                        Debug.Assert(item.height != 0);
                                        checksumDictionary.AddOrUpdate(item.phash,
                                                addValueFactory: (checksum) =>
                                                {
                                                    var list = new List<PHashFileInfo>();
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                },
                                                updateValueFactory: (checksum, list) =>
                                                {
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                });
                                    }
                                    finalResultBag.Clear();
                                    //foreach ((ExtendedFileInfo, Stream) item in streamPairs)
                                    //{
                                    //    item.Item2.Dispose();
                                    //}
                                    streamPairs.Clear();
                                    streamPairs = null;

                                }
                                else
                                {
                                    foreach (var item in dbCollection)
                                    {
                                        //Debug.Assert(item.phash != 0);
                                        Debug.Assert(item.width != 0);
                                        Debug.Assert(item.height != 0);
                                        checksumDictionary.AddOrUpdate(item.phash,
                                                addValueFactory: (checksum) =>
                                                {
                                                    var list = new List<PHashFileInfo>();
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                },
                                                updateValueFactory: (checksum, list) =>
                                                {
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                });
                                    }
                                }
                            }
                            else
                            {
                                var streams = _archiveService.GetStreams(fileInfo, _pHashService.IsSupportedExtension, cancelToken);
                                foreach (var stream in streams)
                                {
                                    toCalculateCollection.Add(stream);
                                }
                            }
                            //GetStreams<ArchiveFileInfo>(fileInfo, toCalculateCollection, cancelToken);

                        }
                        else if (fileInfo.Extension.ToLower() == ".pdf")
                        {
                            if (_searchSetting.UseDB)
                            {
                                var dbCollection = _phashRepository.GetContainerHashes(fileInfo);
                                if (dbCollection == null)
                                {
                                    _logger.LogDebug($"Not found phashes for {fileInfo.Path}, {fileInfo.LastWriteTime}, {fileInfo.Size}");
                                    var streamPairs = _pdfService.GetStreams(fileInfo, cancelToken);

                                    var finalResultBag = new ConcurrentBag<(ExtendedFileInfo efi, ulong phash, int width, int height)>();

                                    var result = Parallel.ForEach(
                                       streamPairs,
                                       new ParallelOptions { MaxDegreeOfParallelism = _numberOfConsumers },
                                       // localInit
                                       () => new List<(ExtendedFileInfo efi, ulong phash, int width, int height)>(), // localInit: Initialize a new local list for each task/partition
                                                                                                                     // body
                                       (item, loopState, localList) => // body: The loop body logic
                                       {
                                           using (item.Item2)
                                           {
                                               var result = _pHashService.CalculatePHash(item.Item2);
                                               if (result.phash.HasValue)
                                                   localList.Add((item.Item1, result.phash.Value, result.width, result.height));
                                               return localList; // Return the updated local list for the next iteration
                                           }
                                       },
                                       (finalLocalList) => // localFinally: Action to combine results
                                       {
                                           foreach (var item in finalLocalList)
                                           {
                                               Debug.Assert(item.phash != 0);
                                               finalResultBag.Add(item);
                                           }
                                       }
                                    );
                                    Debug.Assert(result.IsCompleted);

                                    if (!cancelToken.IsCancellationRequested)
                                    {
                                        _phashRepository.AddContainerStreams(fileInfo, finalResultBag.ToArray());
                                    }
                                    foreach (var item in finalResultBag)
                                    {
                                        Debug.Assert(item.width != 0);
                                        Debug.Assert(item.height != 0);
                                        checksumDictionary.AddOrUpdate(item.phash,
                                                addValueFactory: (checksum) =>
                                                {
                                                    var list = new List<PHashFileInfo>();
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                },
                                                updateValueFactory: (checksum, list) =>
                                                {
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                });
                                    }
                                    finalResultBag.Clear();
                                    //foreach ((ExtendedFileInfo, Stream) item in streamPairs)
                                    //{
                                    //    item.Item2.Dispose();
                                    //}
                                    streamPairs.Clear();
                                    streamPairs = null;

                                }
                                else
                                {
                                    foreach (var item in dbCollection)
                                    {
                                        checksumDictionary.AddOrUpdate(item.phash,
                                                addValueFactory: (checksum) =>
                                                {
                                                    var list = new List<PHashFileInfo>();
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                },
                                                updateValueFactory: (checksum, list) =>
                                                {
                                                    PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi, checksum, item.width, item.height);
                                                    list.Add(pHashFileInfo);
                                                    return list;
                                                });
                                    }
                                }
                            }
                            else
                            {
                                var streamPairs = _pdfService.GetStreams(fileInfo, cancelToken);
                                foreach (var stream in streamPairs)
                                {
                                    toCalculateCollection.Add(stream);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }
            }
        }

        //private void GetStreams<T>(ExtendedFileInfo fileInfo, BlockingCollection<(ExtendedFileInfo, Stream)> toCalculateCollection, CancellationToken cancelToken) where T : ExtendedFileInfo 
        //{         
        //}
       

        //private void CalculateCheckSum(
        //    ReadOnlyCollection<ExtendedFileInfo> collection,
        //    string drive,
        //    IProgress<ProgressDto> progress,
        //    CancellationToken cancelToken)
        //{
        //    //long totalSize = collection.Sum(c => c.Size);
        //    foreach (var data in collection)
        //    {
        //        if (cancelToken.IsCancellationRequested)
        //        {
        //            System.Diagnostics.Debug.WriteLine("CalculateCheckSum was canceled.");
        //            break;
        //        }

        //        //decimal totalSize = blockingCollection.Sum(b => (decimal)b.Size);
        //        progress.Report(new ProgressDto
        //        {
        //            Status = data.Path,
        //            State = "CalculateCheckSum",
        //            PhisicalDrive = drive,
        //            //RemainSize = StringHelper.FormatBytes(totalSize)
        //        });


        //        (ulong phash, int width, int height) = GetCheckSum(data);
        //        if (phash != 0)
        //        {
        //            _checksumDictionary.AddOrUpdate(phash,
        //                addValueFactory: (checksum) =>
        //                {
        //                    var list = new List<PHashFileInfo>();
        //                    PHashFileInfo pHashFileInfo = new PHashFileInfo(data);
        //                    pHashFileInfo.Width = width;
        //                    pHashFileInfo.Height = height;
        //                    list.Add(pHashFileInfo);
        //                    return list;
        //                },
        //                updateValueFactory: (checksum, list) =>
        //                {
        //                    PHashFileInfo pHashFileInfo = new PHashFileInfo(data);
        //                    pHashFileInfo.Width = width;
        //                    pHashFileInfo.Height = height;
        //                    list.Add(pHashFileInfo);
        //                    return list;
        //                });
        //        }

        //            //if (_archiveService.IsArchiveFile(data.Path))
        //            //{
        //            //    var files = _archiveService.GetHashesFromArchive(data);
        //            //    foreach (ExtendedFileInfo file in files)
        //            //    {
        //            //        _checksumDictionary.AddOrUpdate(file.CheckSum,
        //            //             addValueFactory: (checksum) =>
        //            //             {
        //            //                 var list = new List<ExtendedFileInfo>();
        //            //                 list.Add(file);
        //            //                 return list;
        //            //             },
        //            //             updateValueFactory: (checksum, list) =>
        //            //             {
        //            //                 list.Add(file);
        //            //                 return list;
        //            //             });
        //            //    }                      
        //            //}
        //        //}
        //    }

        //    // GetConsumingEnumerable returns the enumerator for the underlying collection.
        //    //var subtractions = 0;
        //    //foreach (var item in bc.GetConsumingEnumerable())
        //    //{
        //    //    Console.WriteLine( $"Consuming tick value {item:D18}");
        //    //}
        //}

        /// <summary>
        /// Return check sum of file. If the checksum does not exist, create it.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //private (ulong phash, int width, int height) GetCheckSum(ExtendedFileInfo fileInfo)
        //{
        //    if (_phashRepository is null)
        //        throw new ArgumentNullException(nameof(_phashRepository));

        //    if (_pHashService.IsSupportedExtension(fileInfo.Extension))
        //    {
        //        if (_searchSetting.UseDB)
        //        {
        //            var lastWriteTime = fileInfo is ArchiveFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
        //            var result = _phashRepository.Get(fileInfo.Path, lastWriteTime, fileInfo.Size);
        //            if (result == null)
        //            {
        //                if (fileInfo is ArchiveFileInfo afi)
        //                {
        //                    (ulong phash2, int width, int height) = _archiveService.CalculateHashInArchive<(ulong phash2, int width, int height)>(afi, _pHashService.CalculatePHash);
        //                    _phashRepository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, phash2, width, height);
        //                    return (phash2, width, height);
        //                }
        //                else
        //                {
        //                    (ulong phash2, int width, int height) = _pHashService.CalculatePHash(fileInfo.Path);
        //                    _phashRepository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, phash2, width, height);
        //                    return (phash2, width, height);
        //                }
        //            }
        //            else
        //                return result.Value;
        //        }
        //        else
        //        {
        //            if (fileInfo is ArchiveFileInfo afi)
        //            {
        //                var result2 = _archiveService.CalculateHashInArchive<(ulong phash2, int width, int height)>(afi, _pHashService.CalculatePHash);
        //                return result2;
        //            }
        //            else
        //            {
        //                var result2 = _pHashService.CalculatePHash(fileInfo.Path);
        //                return result2;
        //            }
        //        }
        //    }
        //    return (0, 0, 0);
        //}

        /// <summary>
        /// Add all files in the requested directory to the files
        /// </summary>
        /// <param name="di">Directory from which to add files</param>
        /// <param name="returnFiles">ArrayList to add files to</param>
        private void AddFilesFromDirectory(
            DirectoryInfo di,
            ref List<ExtendedFileInfo> files,
            bool isRecurse,
            CancellationToken token,
            IProgress<ProgressDto> progress,
            in string phisicalDrive)
        {
            //try
            //{
            //Add subdirectories
            if (isRecurse)
            {
                var directories = di.GetDirectories();
                for (int i = 0; i < directories.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        _logger.LogInformation("AddFiles was canceled.");
                        break;
                    }
                    // Wait on the event to be signaled
                    // or the token to be canceled,
                    // whichever comes first. The token
                    // will throw an exception if it is canceled
                    // while the thread is waiting on the event.
                    try
                    {
                        _mres.Wait(token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Throw immediately to be responsive. The
                        // alternative is to do one more item of work,
                        // and throw on next iteration, because
                        // IsCancellationRequested will be true.
                        _logger.LogInformation("The wait operation was canceled.");
                        throw;
                    }

                    progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = directories[i].FullName, State = "Search" });

                    //if (!_directorySkipList.Contains(directories[i].FullName, StringComparer.OrdinalIgnoreCase))
                    AddFilesFromDirectory(directories[i], ref files, isRecurse, token, progress, phisicalDrive);
                    //else
                    //    Debug.WriteLine(String.Format("Директория {0} есть в списке пропускаемых. Пропускаем.", directories[i].FullName));
                }
            }

            var dFiles = di.GetFiles();
            var container = new DirectoryContainer
            {
                Path = di.FullName,
            };
            var files3 = dFiles.Select(f => new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(f.Length),
                Name = f.Name,
                Path = f.FullName,
                //LastAccessTime = f.LastAccessTime,
                LastWriteTime = f.LastWriteTime,
                DirectoryName = f.DirectoryName,
                Extension = f.Extension,
                Container = container
            });
            container.Files = files3.Select(f => new SimpleFileInfo(f)).ToArray();
            //container.FilesCount = container.Files.Length;
            foreach (var item in files3)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogInformation("AddFiles canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = item.Path, State = "Search" });

                files.Add(item);

                //архивы распаковываем на этапе извлечения Stream
                //if (_archiveService.IsArchiveFile(item.Path))
                //{
                //    var filesInArchive = _dbArchiveService.Get(_searchSetting.UseDB, item, token);
                //    //var filesInArchive = _archiveService.GetInfoFromArchive(item.Path, item, token);
                //    //var res = DeepComparer.DeepEquals(filesInArchive, filesInArchive2);
                //    foreach (ExtendedFileInfo file in filesInArchive)
                //    {
                //        files.Add(file);
                //    }
                //}
            }

        }


        public void Pause()
        {
            _mres.Reset();
        }

        public void Resume()
        {
            _mres.Set();
        }

    }
}
