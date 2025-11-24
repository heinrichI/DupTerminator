using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public class SearcherPhash : SearcherBase<PHashFileInfo>, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly SearchSetting _searchSetting;
        private readonly PHashSettings _pHashSettings;
        private readonly IPhashRepository _phashRepository;
        private readonly IArchiveService _archiveService;
        private readonly DbArchiveService _dbArchiveService;
        private readonly IPHashService _pHashService;
        private readonly IMIHFactory _mihFactory;
        private readonly ILogger<Searcher> _logger;
        private readonly ConcurrentDictionary<ulong, IList<ExtendedFileInfo>> _checksumDictionary = new ConcurrentDictionary<ulong, IList<ExtendedFileInfo>>();


        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);


        public SearcherPhash(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            PHashSettings pHashSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            Service.DbArchiveService dbArchiveService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<Searcher> logger) : base(windowsUtil)
        {
            _locations = locations;
            _searchSetting = searchSetting;
            _pHashSettings = pHashSettings;
            _phashRepository = phashRepository;
            _archiveService = archiveService;
            _dbArchiveService = dbArchiveService;
            _pHashService = pHashService;
            _mihFactory = mIHFactory;
            _logger = logger;
        }

        public async Task<ReadOnlyCollection<PHashDuplicateGroup>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            KeyValuePair<string, List<SearchPath>>[] phisicalDrives = GetPhisicalDrives(_locations);

            (BlockingCollection<ExtendedFileInfo> BlockingCollection, string Drive)[] blockingCollectionByPhisDisks =
                new (BlockingCollection<ExtendedFileInfo>, string drive)[phisicalDrives.Length];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                blockingCollectionByPhisDisks[i] = new(
                    new BlockingCollection<ExtendedFileInfo>(), phisicalDrives[i].Key);
            }

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
            //for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            //{
                //int temp = i;
                //tasksByDrivers[phisicalDrives.Length + temp] = Task.Run(() => SplitByDrive(result[temp], blockingCollectionByPhisDisks[temp].BlockingCollection, cancelToken));
            //}


            for (int i = 0; i < phisicalDrives.Length; i++)
            {
                int temp = i;
                tasksByDrivers[temp] = Task.Run(() => CalculateCheckSum(
                        result[temp],
                        phisicalDrives[temp].Key, progress, cancelToken))
                     .ContinueWith(t => progress?.Report(new ProgressDto
                     {
                         PhisicalDrive = phisicalDrives[temp].Key,
                         Status = "CalculateCheckSum ended",
                         State = "CalculateCheckSum",
                         RemainSize = string.Empty,
                     }), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

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


            await Task.WhenAll(tasksByDrivers);

            Debug.Assert(_checksumDictionary.Count > 0, "Файлов нет!");

            Dictionary<ExtendedFileInfo, int> groupIndexByInfo = new Dictionary<ExtendedFileInfo, int>();
            HashSet<ulong> skip = new HashSet<ulong>();
            List<PHashDuplicateGroup> duplicateGroups = new List<PHashDuplicateGroup>();
            using (var mih = _mihFactory.Create())
            {
                progress?.Report(new ProgressDto
                {
                    Status = "Start train MIH",
                    State = "TrainMIH",
                    RemainSize = string.Empty,
                });

                mih.Update(_checksumDictionary);
                mih.Train(wordLength:_pHashSettings.WordLength, threshold: _pHashSettings.HammingDistance);

                progress?.Report(new ProgressDto
                {
                    Status = "Ended train MIH",
                    State = "EndedTrainMIH",
                    RemainSize = string.Empty,
                });


                foreach (var pair in _checksumDictionary)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("MIH quering was canceled.");
                        break;
                    }

                    if (skip.Contains(pair.Key))
                        continue;

                    progress?.Report(new ProgressDto
                    {
                        Status = pair.Value.First().Path,
                        State = "QueryMIH",
                        RemainSize = string.Empty,
                    });

                    PHashDuplicateGroup duplicateGroup = null;
                    //if (pair.Value.Count > 1)
                    //{
                    duplicateGroup = GetDuplicateGroup(groupIndexByInfo, duplicateGroups, pair.Value.First());
                    foreach (var file in pair.Value)
                    {
                        if (!duplicateGroup.ContainsPath(file.Path))
                        {
                            PHashFileInfoSearchItem? fileInfo = new PHashFileInfoSearchItem(file, PHashFileInfoSearchItem.SearchType.Seed);
                            duplicateGroup.Add(fileInfo);
                        }
                    }


                    (ulong Hash, List<ExtendedFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(pair.Key).ToArray();
                    skip.Add(pair.Key);
                    if (resultQuery.Length <= 1)
                        continue;
                    foreach ((ulong Hash, List<ExtendedFileInfo> FileInfos, int HammingDistance) item in resultQuery)
                    {
                        foreach (var fileItem in item.FileInfos)
                        {
                            //if (fileItem == pair.Value.First())
                            if (duplicateGroup.ContainsPath(fileItem.Path))
                                continue;
                            PHashFileInfoSearchItem isi;
                            if (item.Hash == pair.Key)
                            {
                                Debug.Assert(item.HammingDistance == 0);
                                isi = new PHashFileInfoSearchItem(fileItem, PHashFileInfoSearchItem.SearchType.Seed);
                            }
                            else
                                isi = new PHashFileInfoSearchItem(fileItem, item.HammingDistance);

                            //if (duplicateGroup is null)
                            //    duplicateGroup = GetDuplicateGroup(groupIndexByInfo, duplicateGroups, fileItem);
                            duplicateGroup.Add(isi);
                            groupIndexByInfo[fileItem] = duplicateGroups.IndexOf(duplicateGroup);
                        }
                    }
                }
            }

            //var list = duplicateGroups.Select(d => new DuplicateGroup(Guid.NewGuid().ToString(), d.ToList())).ToList();
            return new ReadOnlyCollection<PHashDuplicateGroup>(duplicateGroups.Where(d => d.Count > 1).ToList());
            
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
                AddFiles(di, ref files, directory.SearchInSubFolder, token, progress, phisicalDrive);
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
            if (_archiveService.IsArchiveFile(file.Path))
            {
                var filesInArchive = _dbArchiveService.Get(_searchSetting.UseDB, f, token);
                foreach (ExtendedFileInfo archFile in filesInArchive)
                {
                    files.Add(archFile);
                }
            }
        }

        private void CalculateCheckSum(
            ReadOnlyCollection<ExtendedFileInfo> collection,
            string drive,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            //long totalSize = collection.Sum(c => c.Size);
            foreach (var data in collection)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("CalculateCheckSum was canceled.");
                    break;
                }

                //decimal totalSize = blockingCollection.Sum(b => (decimal)b.Size);
                progress.Report(new ProgressDto
                {
                    Status = data.Path,
                    State = "CalculateCheckSum",
                    PhisicalDrive = drive,
                    //RemainSize = StringHelper.FormatBytes(totalSize)
                });


                ulong checksum = GetCheckSum(data);
                if (checksum != 0)
                {
                    _checksumDictionary.AddOrUpdate(checksum,
                        addValueFactory: (checksum) =>
                        {
                            var list = new List<ExtendedFileInfo>();
                            list.Add(data);
                            return list;
                        },
                        updateValueFactory: (checksum, list) =>
                        {
                            list.Add(data);
                            return list;
                        });
                }

                    //if (_archiveService.IsArchiveFile(data.Path))
                    //{
                    //    var files = _archiveService.GetHashesFromArchive(data);
                    //    foreach (ExtendedFileInfo file in files)
                    //    {
                    //        _checksumDictionary.AddOrUpdate(file.CheckSum,
                    //             addValueFactory: (checksum) =>
                    //             {
                    //                 var list = new List<ExtendedFileInfo>();
                    //                 list.Add(file);
                    //                 return list;
                    //             },
                    //             updateValueFactory: (checksum, list) =>
                    //             {
                    //                 list.Add(file);
                    //                 return list;
                    //             });
                    //    }                      
                    //}
                //}
            }

            // GetConsumingEnumerable returns the enumerator for the underlying collection.
            //var subtractions = 0;
            //foreach (var item in bc.GetConsumingEnumerable())
            //{
            //    Console.WriteLine( $"Consuming tick value {item:D18}");
            //}
        }

        /// <summary>
        /// Return check sum of file. If the checksum does not exist, create it.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private ulong GetCheckSum(ExtendedFileInfo fileInfo)
        {
            if (_phashRepository is null)
                throw new ArgumentNullException(nameof(_phashRepository));

            if (_pHashService.IsSupportedExtension(fileInfo.Extension))
            {
                if (_searchSetting.UseDB)
                {
                    var lastWriteTime = fileInfo is ArchiveFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
                    ulong? phash = _phashRepository.Get(fileInfo.Path, lastWriteTime, fileInfo.Size);
                    if (phash == null)
                    {
                        if (fileInfo is ArchiveFileInfo afi)
                        {
                            phash = _archiveService.CalculateHashInArchive<ulong>(afi, _pHashService.CalculatePHash);
                        }
                        else
                        {
                            phash = _pHashService.CalculatePHash(fileInfo.Path);
                        }
                        _phashRepository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, phash.Value);
                        return phash.Value;
                    }
                    else
                        return phash.Value;
                }
                else
                {
                    if (fileInfo is ArchiveFileInfo afi)
                    {
                        ulong phash = _archiveService.CalculateHashInArchive<ulong>(afi, _pHashService.CalculatePHash);
                        return phash;
                    }
                    else
                    {
                        ulong phash = _pHashService.CalculatePHash(fileInfo.Path);
                        return phash;
                    }
                }
            }
            return 0;



            //if (fileInfo.CheckSum == null)
            //{
            //    if (_searchSetting.UseDB)
            //    {
            //        //System.Diagnostics.Debug.WriteLine("CheckSum _dbManager.Active=" + _dbManager.Active);
            //        string md5 = string.Empty;
            //        var lastWriteTime = fileInfo is ArchiveFileInfo? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
            //        md5 = _dbManager.ReadMD5(fileInfo.CombinedPath, lastWriteTime, fileInfo.Size);
            //        if (string.IsNullOrEmpty(md5))
            //        {
            //            //System.Diagnostics.Debug.WriteLine(String.Format("md5 not found in DB for file {0}, lastwrite: {1}, length: {2}", _fi.FullName, _fi.LastWriteTime, _fi.Length));
            //            if (fileInfo is ArchiveFileInfo afi)
            //            {
            //                fileInfo.CheckSum = _archiveService.CalculateHashInArchive(afi);
            //            }
            //            else
            //            {
            //                fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
            //            }
            //            _dbManager.Add(fileInfo.CombinedPath, lastWriteTime, fileInfo.Size, fileInfo.CheckSum);
            //            //_dbManager.Update(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
            //        }
            //        else
            //            fileInfo.CheckSum = md5;
            //    }
            //    else
            //    {
            //        if (fileInfo is ArchiveFileInfo afi)
            //        {
            //            fileInfo.CheckSum = _archiveService.CalculateHashInArchive(afi);
            //        }
            //        else
            //        {
            //            fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
            //        }
            //    }
            //}
            //return fileInfo.CheckSum;
        }

        /// <summary>
        /// Add all files in the requested directory to the files
        /// </summary>
        /// <param name="di">Directory from which to add files</param>
        /// <param name="returnFiles">ArrayList to add files to</param>
        private void AddFiles(
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
                    AddFiles(directories[i], ref files, isRecurse, token, progress, phisicalDrive);
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
                if (_archiveService.IsArchiveFile(item.Path))
                {
                    var filesInArchive = _dbArchiveService.Get(_searchSetting.UseDB, item, token);
                    //var filesInArchive = _archiveService.GetInfoFromArchive(item.Path, item, token);
                    //var res = DeepComparer.DeepEquals(filesInArchive, filesInArchive2);
                    foreach (ExtendedFileInfo file in filesInArchive)
                    {
                        files.Add(file);
                    }
                }
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
