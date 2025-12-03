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
    public class SearcherPhash : SearcherBase<ExtendedFileInfo>, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly SearchSetting _searchSetting;
        private readonly PHashSettings _pHashSettings;
        private readonly IPhashRepository _phashRepository;
        private readonly IArchiveService _archiveService;
        //private readonly DbArchiveService _dbArchiveService;
        private readonly IPHashService _pHashService;
        private readonly IMIHFactory _mihFactory;
        private readonly ILogger<Searcher> _logger;
        private readonly ConcurrentDictionary<ulong, IList<PHashFileInfo>> _checksumDictionary = new ConcurrentDictionary<ulong, IList<PHashFileInfo>>();
        private readonly Stopwatch _stopwatch = new();

        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);

        private readonly int _numberOfConsumers = Environment.ProcessorCount;
        // Use the shared singleton instance of MemoryPool<int>
        //MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;

        public SearcherPhash(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            PHashSettings pHashSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            //Service.DbArchiveService dbArchiveService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<Searcher> logger) : base(windowsUtil)
        {
            _locations = locations;
            _searchSetting = searchSetting;
            _pHashSettings = pHashSettings;
            _phashRepository = phashRepository;
            _archiveService = archiveService;
            //_dbArchiveService = dbArchiveService;
            _pHashService = pHashService;
            _mihFactory = mIHFactory;
            _logger = logger;
        }

        public async Task<ReadOnlyCollection<PHashDuplicateGroup>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            _stopwatch.Restart();

            KeyValuePair<string, List<SearchPath>>[] phisicalDrives = GetPhisicalDrives(_locations);

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
                       State = "Search",
                       Status = "Search ended"
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
                        phisicalDrives[temp].Key, blockingCollection, progress, cancelToken))
                     .ContinueWith(t => progress?.Report(new ProgressDto
                     {
                         PhisicalDrive = phisicalDrives[temp].Key,
                         Status = "FillStreamBuffer ended",
                         State = "FillStreamBuffer",
                         RemainSize = string.Empty,
                     }), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            // Wait for both producer and all consumers to complete
            _ = Task.WhenAll(tasksByDrivers).ContinueWith(t => blockingCollection.CompleteAdding(), TaskContinuationOptions.ExecuteSynchronously); 



            //Parallel.ForEach(blockingCollection,
            //new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            //item =>
            //{
            //    // Process the item
            //    //Console.WriteLine($"Processing item {item} on Thread {Thread.CurrentThread.ManagedThreadId}");
            //    CalculateCheckSum(blockingCollection);
            //});

            Task[] consumers = new Task[_numberOfConsumers];
            for (int i = 0; i < _numberOfConsumers; i++)
            {
                int consumerId = i;
                consumers[i] = Task.Run(() => // Task.Run is preferred over Task.Factory.StartNew
                {
                    CalculateCheckSum(blockingCollection, progress, cancelToken);
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

            //Debug.Assert(_checksumDictionary.Count > 0, "Файлов нет!");

            if (_checksumDictionary.Any())
            {
                Dictionary<ExtendedFileInfo, int> groupIndexByInfo = new Dictionary<ExtendedFileInfo, int>();
                HashSet<ulong> skip = new HashSet<ulong>();
                List<PHashDuplicateGroup> duplicateGroups = new List<PHashDuplicateGroup>();
                using (var mih = _mihFactory.Create())
                {
                    Debug.WriteLine("Start train MIH");
                    progress?.Report(new ProgressDto
                    {
                        Status = "Start train MIH",
                        State = "TrainMIH",
                        RemainSize = string.Empty,
                    });

                    mih.Update(_checksumDictionary);
                    mih.Train(wordLength: _pHashSettings.WordLength, threshold: _pHashSettings.HammingDistance);

                    progress?.Report(new ProgressDto
                    {
                        Status = "Ended train MIH",
                        State = "EndedTrainMIH",
                        RemainSize = string.Empty,
                    });
                    Debug.WriteLine("Ended train MIH");


                    foreach (var pair in _checksumDictionary)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("MIH quering was canceled.");
                            break;
                        }

                        if (skip.Contains(pair.Key))
                            continue;

                        Debug.WriteLine("QueryMIH" + pair.Value.First().FileInfo.Path);
                        progress?.Report(new ProgressDto
                        {
                            Status = pair.Value.First().FileInfo.Path,
                            State = "QueryMIH",
                            RemainSize = string.Empty,
                        });

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

                //if (GCSettings.LargeObjectHeapCompactionMode != GCLargeObjectHeapCompactionMode.CompactOnce)   // no-op if already set
                //{
                //    GCSettings.LargeObjectHeapCompactionMode =
                //        GCLargeObjectHeapCompactionMode.CompactOnce;
                //}

                _stopwatch.Stop();
                _logger.LogInformation($"ElapsedTime: {_stopwatch.Elapsed}");


                //var list = duplicateGroups.Select(d => new DuplicateGroup(Guid.NewGuid().ToString(), d.ToList())).ToList();
                var duplicateGroupsFiltered = duplicateGroups.Where(d => d.Count > 1).ToList();
                return new ReadOnlyCollection<PHashDuplicateGroup>(duplicateGroupsFiltered);
            }

            return new ReadOnlyCollection<PHashDuplicateGroup>(new PHashDuplicateGroup[0]);


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

        private void CalculateCheckSum(BlockingCollection<(ExtendedFileInfo, Stream)> blockingCollection, IProgress<ProgressDto> progress,
           CancellationToken cancelToken)
        {
            foreach (var item in blockingCollection.GetConsumingEnumerable())
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("MIH quering was canceled.");
                    break;
                }

                progress?.Report(new ProgressDto
                {
                    Status = item.Item1.Path,
                    State = "CalculateCheckSum",
                    RemainSize = string.Empty,
                });

                using (item.Item2)
                {
                    var result = _pHashService.CalculatePHash(item.Item2);

                    if (_searchSetting.UseDB)
                    {
                        var lastWriteTime = item.Item1 is ArchiveFileInfo ? item.Item1.Container.LastWriteTime : item.Item1.LastWriteTime;
                        _phashRepository.Add(item.Item1.Path, lastWriteTime, item.Item1.Size, result.phash, result.width, result.height);
                    }
                    _checksumDictionary.AddOrUpdate(result.phash,
                    addValueFactory: (checksum) =>
                    {
                        var list = new List<PHashFileInfo>();
                        PHashFileInfo pHashFileInfo = new PHashFileInfo(item.Item1);
                        pHashFileInfo.Width = result.width;
                        pHashFileInfo.Height = result.height;
                        list.Add(pHashFileInfo);
                        return list;
                    },
                    updateValueFactory: (checksum, list) =>
                    {
                        PHashFileInfo pHashFileInfo = new PHashFileInfo(item.Item1);
                        pHashFileInfo.Width = result.width;
                        pHashFileInfo.Height = result.height;
                        list.Add(pHashFileInfo);
                        return list;
                    });
                }
            }
        }


        // из разных потоков
        private ReadOnlyCollection<ExtendedFileInfo> SearchFileOnPhisicalDrive(
            IProgress<ProgressDto> progress,
            in string phisicalDrive,
            IEnumerable<SearchPath> locations,
            CancellationToken token)
        {
            System.Diagnostics.Debug.WriteLine($"SearchFileOnPhisicalDrive {phisicalDrive} start.");
            List<ExtendedFileInfo> files = new List<ExtendedFileInfo>();
            foreach (var directory in locations.Where(p => p.IsDirectory))
            {
                if (token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("SearchFileOnPhisicalDrive was canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = directory.Path, State = "Search" });

                if (!Directory.Exists(directory.Path))
                    throw new Exception("Directory does not exists!");
                DirectoryInfo di = new System.IO.DirectoryInfo(directory.Path);
                AddFilesFromDirectory(di, ref files, directory.SearchInSubFolder, token, progress, phisicalDrive);
            }
            foreach (var file in locations.Where(p => !p.IsDirectory))
            {
                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = file.Path, State = "Search" });

                AddFile(file, ref files, token, progress, phisicalDrive);
            }

            //progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = string.Empty, State = "Search ended" });

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }

        private void AddFile(SearchPath file, ref List<ExtendedFileInfo> files, CancellationToken token, IProgress<ProgressDto> progress, string phisicalDrive)
        {
            if (token.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine("AddFiles canceled.");
                return;
            }

            progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = file.Path, State = "Search" });

            var fi = new FileInfo(file.Path);
            var f = new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(fi.Length),
                Name = fi.Name,
                Path = fi.FullName,
                LastAccessTime = fi.LastAccessTime,
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
           IProgress<ProgressDto> progress,
           CancellationToken cancelToken)
        {
            if (_phashRepository is null)
                throw new ArgumentNullException(nameof(_phashRepository));

            decimal totalSize = collection.Sum(b => (decimal)b.Size);
            decimal calculatedSize = 0;
            foreach (var fileInfo in collection)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("CalculateCheckSum was canceled.");
                    break;
                }

                progress.Report(new ProgressDto
                {
                    Status = fileInfo.Path,
                    State = "FillStreamBuffer",
                    PhisicalDrive = drive,
                    RemainSize = StringHelper.FormatBytes(totalSize - calculatedSize)
                });
                calculatedSize += fileInfo.Size;


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
                            _checksumDictionary.AddOrUpdate(result.Value.phash,
                            addValueFactory: (checksum) =>
                            {
                                var list = new List<PHashFileInfo>();
                                PHashFileInfo pHashFileInfo = new PHashFileInfo(fileInfo);
                                pHashFileInfo.Width = result.Value.width;
                                pHashFileInfo.Height = result.Value.height;
                                list.Add(pHashFileInfo);
                                return list;
                            },
                            updateValueFactory: (checksum, list) =>
                            {
                                PHashFileInfo pHashFileInfo = new PHashFileInfo(fileInfo);
                                pHashFileInfo.Width = result.Value.width;
                                pHashFileInfo.Height = result.Value.height;
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

                            // The master list to collect all results
                            var finalResultCollection = new List<(ArchiveFileInfo efi, ulong phash, int width, int height)>(streamPairs.Count);
                            // Local bag for combining results without a global lock
                            var localBag = new ConcurrentBag<(ArchiveFileInfo efi, ulong phash, int width, int height)>();

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
                                       var result = _pHashService.CalculatePHash(item.Item2);
                                       localList.Add((item.Item1, result.phash, result.width, result.height));
                                       return localList; // Return the updated local list for the next iteration
                                   }
                               },
                               (finalLocalList) => // localFinally: Action to combine results
                               {
                                   finalResultCollection.AddRange(finalLocalList);
                               }
                            );

                            _phashRepository.AddContainerStreams(fileInfo, finalResultCollection.ToArray());
                            foreach (var item in finalResultCollection)
                            {
                                _checksumDictionary.AddOrUpdate(item.phash,
                                        addValueFactory: (checksum) =>
                                        {
                                            var list = new List<PHashFileInfo>();
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi);
                                            pHashFileInfo.Width = item.width;
                                            pHashFileInfo.Height = item.height;
                                            list.Add(pHashFileInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checksum, list) =>
                                        {
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi);
                                            pHashFileInfo.Width = item.width;
                                            pHashFileInfo.Height = item.height;
                                            list.Add(pHashFileInfo);
                                            return list;
                                        });
                            }
                            localBag.Clear();
                            finalResultCollection = null;
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
                                _checksumDictionary.AddOrUpdate(item.phash,
                                        addValueFactory: (checksum) =>
                                        {
                                            var list = new List<PHashFileInfo>();
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi);
                                            pHashFileInfo.Width = item.width;
                                            pHashFileInfo.Height = item.height;
                                            list.Add(pHashFileInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checksum, list) =>
                                        {
                                            PHashFileInfo pHashFileInfo = new PHashFileInfo(item.efi);
                                            pHashFileInfo.Width = item.width;
                                            pHashFileInfo.Height = item.height;
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
                }
            //if (fileInfo is ArchiveFileInfo afi)
            //{

            //    var stream = _archiveService.GetStream(afi);
            //    blockingCollection.Add((fileInfo, stream));
            //    //_phashRepository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, phash2, width, height);

            //}
            //if (_searchSetting.UseDB)
            //{
            //    var lastWriteTime = fileInfo is ArchiveFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
            //    var result = _phashRepository.Get(fileInfo.Path, lastWriteTime, fileInfo.Size);

            //}
            //else
            //{
            //    if (fileInfo is ArchiveFileInfo afi)
            //    {
            //        var stream = _archiveService.GetStream(afi);
            //        blockingCollection.Add((fileInfo, stream));
            //    }
            //    else
            //    {
            //        var memoryStream = new MemoryStream();
            //        using (var fileStream = System.IO.File.OpenRead(fileInfo.Path))
            //        {
            //            // Copy the entire contents of the FileStream into the MemoryStream
            //            fileStream.CopyTo(memoryStream);
            //        }

            //        // Optional: Reset the position to the beginning of the MemoryStream for subsequent read operations
            //        memoryStream.Position = 0;
            //        blockingCollection.Add((fileInfo, memoryStream));
            //    }
            //}

            //}
        }
    }

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
                        System.Diagnostics.Debug.WriteLine("AddFiles was canceled.");
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
                        System.Diagnostics.Debug.WriteLine("The wait operation was canceled.");
                        throw;
                    }

                    progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = directories[i].FullName, State = "Search" });

                    //if (!_directorySkipList.Contains(directories[i].FullName, StringComparer.OrdinalIgnoreCase))
                    AddFilesFromDirectory(directories[i], ref files, isRecurse, token, progress, phisicalDrive);
                    //else
                    //    Debug.WriteLine(String.Format("Директория {0} есть в списке пропускаемых. Пропускаем.", directories[i].FullName));
                }
            }

            var files3 = di.GetFiles().Select(f => new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(f.Length),
                Name = f.Name,
                Path = f.FullName,
                LastAccessTime = f.LastAccessTime,
                LastWriteTime = f.LastWriteTime,
                DirectoryName = f.DirectoryName,
                Extension = f.Extension,
            });
            foreach (var item in files3)
            {
                if (token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("AddFiles canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = item.Path, State = "Search" });

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

        public void Dispose()
        {
            //_cts?.Dispose();
        }

        //public void Cancell()
        //{
        //    // Token can only be canceled once.
        //    _cts.Cancel();
        //}

        public void Pause()
        {
            _mres.Reset();
        }

        public void Resume()
        {
            _mres.Set();
        }


        internal class Pair<T1, T2>
        {
            public Pair(T1 key, T2 extendedFileInfos)
            {
                Key = key;
                Value = extendedFileInfos;
            }

            public T1 Key { get; internal set; }
            public T2 Value { get; internal set; }
        }
    }
}
