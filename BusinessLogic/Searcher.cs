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
using System.Xml.Linq;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Service;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DupTerminator.BusinessLogic
{
    public class Searcher : SearcherBase<ExtendedFileInfo>, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly SearchSetting _searchSetting;
        private readonly IDBManager _dbManager;
        private readonly IArchiveService _archiveService;
        private readonly DbArchiveService _dbArchiveService;
        private readonly ILogger<Searcher> _logger;
        private readonly ConcurrentDictionary<string, IList<ExtendedFileInfo>> _checksumDictionary = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
        //public ReadOnlyCollection<DuplicateGroup> Duplicates { get; private set; }

        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);

        //private CancellationTokenSource _cts;

        //IProgress<Tuple<int, string>> _progressSearchFile = new Progress<Tuple<int, string>>();

        //IProgress<Tuple<int, string>> _progressCalculateDuplicate = new Progress<Tuple<int, string>>();

        public Searcher(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            IDBManager dbManager,
            IWindowsUtil windowsUtil,
            //IProgress<ProgressDto> progress,
            //CancellationToken cancellationToken,
            IArchiveService archiveService,
            Service.DbArchiveService dbArchiveService,
            ILogger<Searcher> logger) : base(windowsUtil)
        {
            _locations = locations;
            _searchSetting = searchSetting;
            _dbManager = dbManager;
            //_progress = progress;
            _archiveService = archiveService;
            _dbArchiveService = dbArchiveService;
            _logger = logger;
        }

        public async Task<ReadOnlyCollection<DuplicateGroup>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
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
                //tasksSearch[i] = Task.Factory.StartNew<ReadOnlyCollection<ExtendedFileInfo>>(
                //    () => SearchFileOnPhisicalDrive(progress, phisicalDrives[temp].Key, phisicalDrives[temp].Value, cancelToken),
                //    cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
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

            //IEnumerable<ReadOnlyCollection<ExtendedFileInfo>> results = await Task.WhenAll(tasks);
            ReadOnlyCollection<ExtendedFileInfo>[]? result = await Task.WhenAll(tasksSearch);

            //получаем список файлов
            //отсеиваем только с одинаковыми размерами
            //считаем для них хещ



            Task[] tasksCompareBySizeAndCalculateCheckSum = new Task[phisicalDrives.Length * 2];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                int temp = i;
                //tasksCompareBySize[phisicalDrives.Length + temp] = Task.Factory.StartNew((d) =>
                //    CompareBySize(result[temp], blockingCollectionByPhisDisks[temp].BlockingCollection, cancelToken),
                //    cancelToken,
                //    TaskCreationOptions.LongRunning);
                tasksCompareBySizeAndCalculateCheckSum[phisicalDrives.Length + temp] = Task.Run(() => CompareBySize(result[temp], blockingCollectionByPhisDisks[temp].BlockingCollection, cancelToken));
            }

            for (int i = 0; i < phisicalDrives.Length; i++)
            {
                int temp = i;
                //tasksCompareBySize[temp] = Task.Factory.StartNew(
                //    () => CalculateCheckSum(
                //        blockingCollectionByPhisDisks[temp].BlockingCollection,
                //        blockingCollectionByPhisDisks[temp].Drive, progress, cancelToken),
                //        cancelToken,
                //        TaskCreationOptions.LongRunning,
                //        TaskScheduler.Current);
                tasksCompareBySizeAndCalculateCheckSum[temp] = Task.Run(() => CalculateCheckSum(
                        blockingCollectionByPhisDisks[temp].BlockingCollection,
                        blockingCollectionByPhisDisks[temp].Drive, progress, cancelToken))
                     .ContinueWith(t => progress?.Report(new ProgressDto
                     {
                         PhisicalDrive = phisicalDrives[temp].Key,
                         Status = "CalculateCheckSum ended",
                         State = "CalculateCheckSum",
                         RemainSize = string.Empty,
                     }), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            var sr = await Task.WhenAll(tasksCompareBySizeAndCalculateCheckSum).ContinueWith((tasks2) =>
            {
                foreach (var item in blockingCollectionByPhisDisks)
                {
                    item.BlockingCollection.Dispose();
                }
                IEnumerable<DuplicateGroup>? duplicates = _checksumDictionary
                    .Where(pair => pair.Value.Count > 1)
                    .Select(pair => new DuplicateGroup(pair.Key, pair.Value));
                //.OrderByDescending(d => d.Files.Any(f => f.Container is null));

                var withoutContainer = duplicates.SelectMany(f => f.Files).Where(d => d.Container is null);
                var d2 = duplicates.Where(d => d.Files.Any(f => withoutContainer.Any(c => f.Container is not null && c.Path == f.Container.Path)));
                if (d2 != null && d2.Any())
                {
                    _logger.LogInformation($"Контейнеров с дублями: {d2.Count()}");
                }

                return new ReadOnlyCollection<DuplicateGroup>(duplicates.Except(d2).ToList());

                //проверяем сначала сами контейнеры, если есть совпадающие то откидываем все файлы из них

                //var duplicatesDict = duplicates.ToDictionary(k => k.Checksum);

                ////если все файлы из одного контейнера совпадают, то удаляем их и оставляем только контейнер
                //var fileContainers = duplicates
                //    .SelectMany(d => d.Files)
                //    .GroupBy(f => f.Container)
                //    .Select(g => new Pair<ExtendedFileInfo, IList<ExtendedFileInfo>>(g.Key, g.ToList()))
                //    .ToArray();
                //foreach (Pair<ExtendedFileInfo, IList<ExtendedFileInfo>>? container in fileContainers)
                //{
                //    if (container.Key is null)
                //        continue;

                //    if (cancelToken.IsCancellationRequested)
                //    {
                //        System.Diagnostics.Debug.WriteLine("Container grouping was cancelled.");
                //        break;
                //    }

                //    ExtendedFileInfo? firstFile = container.Value.FirstOrDefault();
                //    if (firstFile != null)
                //    {
                //        DuplicateGroup group = duplicatesDict[firstFile.CheckSum];
                //        //если файлы лежат не только в контейнере, проверить не совпадают ли все файлы из контейнера с файлами в директории,
                //        //если совпадают - создать виртуальный контейнер
                //        var duplCandidates = group.Files
                //            .Where(f => f.Container?.CombinedPath != container?.Key?.CombinedPath)
                //            .GroupBy(f => f.Container);
                //        foreach (IGrouping<ExtendedFileInfo, ExtendedFileInfo> duplCandidate in duplCandidates)
                //        {
                //            if (duplCandidate.Key is null)
                //            {
                //                //этот файл лежит просто в директории
                //                var filesInDirectory = duplicates.SelectMany(d => d.Files).Where(f => f.DirectoryName == duplCandidate.First().DirectoryName);
                //                foreach (var file in container.Value)
                //                {
                //                    if (!filesInDirectory.Any(f => f.Size == file.Size && f.CheckSum == f.CheckSum))
                //                        break;
                //                }
                //                //добавить виртуальный контейнер
                //                DuplicateContainer container2 = new DuplicateContainer();
                //            }
                //            else
                //            {
                //                Pair<ExtendedFileInfo, IList<ExtendedFileInfo>> duplicateContainer = fileContainers
                //                    .Single(c => c.Key != null && c.Key.CombinedPath == duplCandidate.Key?.CombinedPath);
                //                if (duplicateContainer.Value.Count == container.Value.Count)
                //                {
                //                    bool sequenceEqual = duplicateContainer.Value.SequenceEqual(container.Value, new CheckSumComparer());
                //                    if (sequenceEqual)
                //                    {
                //                        container.Value.Clear();
                //                        duplicateContainer.Value.Clear();
                //                        //foreach (var item in duplicates)
                //                        //{
                //                        //if (item.Files.First().Container.CombinedPath == can.Key.CombinedPath || item.Files.First().Container.CombinedPath == container.Key.CombinedPath)
                //                        //{
                //                        //    item.Files.RemoveAll(f => f.CombinedPath == can.Key.CombinedPath && can.Any(c => c.Name == f.Name));
                //                        //    item.Files.RemoveAll(f => f.CombinedPath == container.Key.CombinedPath && container.Any(c => c.Name == f.Name));
                //                        //}
                //                        //}
                //                    }
                //                }
                //                else
                //                {
                //                    _logger.LogDebug($"У кандитаного контненера {duplCandidate} не совпадает количество файлов");
                //                }
                //            }
                //        }
                //    }
                //}

                //var duplicates2 = new ReadOnlyCollection<DuplicateGroup>(fileContainers
                //    .SelectMany(c => c.Value)
                //    .GroupBy(f => f.CheckSum)
                //    .Select(f => new DuplicateGroup(f.Key, (IList<ExtendedFileInfo>)f))
                //    .ToList());

                //return duplicates2;
            });

            return sr;
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

                DirectoryInfo di = new System.IO.DirectoryInfo(directory.Path);
                AddFiles(di, ref files, directory.SearchInSubFolder, token, progress, phisicalDrive);
            }

            //progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = string.Empty, State = "Search ended" });

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }


        private void CalculateCheckSum(
            BlockingCollection<ExtendedFileInfo> blockingCollection,
            string drive,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            if (blockingCollection == null)
                throw new ArgumentNullException(nameof(blockingCollection));

            //var timeout = TimeSpan.FromMilliseconds(1000);
            //int localSum = 0;
            //while (bc.TryTake(out ExtendedFileInfo localItem, timeout))
            //{
            //    localSum++;
            //}

            while (!blockingCollection.IsCompleted && !cancelToken.IsCancellationRequested)
            {
                ExtendedFileInfo data = null;
                // Blocks if number.Count == 0
                // IOE means that Take() was called on a completed collection.
                // Some other thread can call CompleteAdding after we pass the
                // IsCompleted check but before we call Take. 
                // In this example, we can simply catch the exception since the 
                // loop will break on the next iteration.

                try
                {
                    data = blockingCollection.Take();
                }
                catch (InvalidOperationException ex)
                {
                    Task.Delay(500);
                }

                if (data != null)
                {
                    decimal totalSize = blockingCollection.Sum(b => (decimal)b.Size);
                    progress.Report(new ProgressDto { Status = data.Name, State = "CalculateCheckSum",
                        PhisicalDrive = drive,
                        RemainSize = StringHelper.FormatBytes(totalSize) });

                    string checksum = GetCheckSum(data);
                    Debug.Assert(checksum is not null);
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
                }
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
        private string GetCheckSum(ExtendedFileInfo fileInfo)
        {
            if (_dbManager is null)
                throw new ArgumentNullException(nameof(_dbManager));

            if (fileInfo.CheckSum == null)
            {
                if (_searchSetting.UseDB)
                {
                    //System.Diagnostics.Debug.WriteLine("CheckSum _dbManager.Active=" + _dbManager.Active);
                    string md5 = string.Empty;
                    DateTime lastWriteTime = fileInfo is ArchiveFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
                    md5 = _dbManager.ReadMD5(fileInfo.Path, lastWriteTime, fileInfo.Size);
                    if (string.IsNullOrEmpty(md5))
                    {
                        //System.Diagnostics.Debug.WriteLine(String.Format("md5 not found in DB for file {0}, lastwrite: {1}, length: {2}", _fi.FullName, _fi.LastWriteTime, _fi.Length));
                        if (fileInfo is ArchiveFileInfo afi)
                        {
                            fileInfo.CheckSum = _archiveService.CalculateHashInArchive<string?>(afi, HashHelper.CreateMD5Checksum);
                        }
                        else
                        {
                            fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
                        }
                        _dbManager.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, fileInfo.CheckSum);
                        //_dbManager.Update(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
                    }
                    else
                        fileInfo.CheckSum = md5;
                }
                else
                {
                    if (fileInfo is ArchiveFileInfo afi)
                    {
                        fileInfo.CheckSum = _archiveService.CalculateHashInArchive<string?>(afi, HashHelper.CreateMD5Checksum);
                    }
                    else
                    {
                        fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
                    }
                }
            }
            return fileInfo.CheckSum;
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

                //расширения
                //if (_searchSetting.IncludePattern.Count > 0)
                //{
                //    foreach (string pattern in _searchSetting.IncludePattern)
                //    {
                //        var files2 = di.GetFiles(pattern, SearchOption.TopDirectoryOnly).Select(f => new ExtendedFileInfo(f));
                //        foreach (var item in files2)
                //        {
                //            files.Add(item);
                //        }
                //    }
                //}
                //else
                //{
                //    var files2 = di.GetFiles().Select(f => new ExtendedFileInfo(f));
                //    foreach (var item in files2)
                //    {
                //        files.Add(item);
                //    }
                //}

                //if (_searchSetting.ExcludePattern.Count > 0)
                //{
                //    List<ExtendedFileInfo> excludeFiles = new List<ExtendedFileInfo>();

                //    foreach (string patternExclude in _searchSetting.ExcludePattern)
                //        excludeFiles.AddRange(di.GetFiles(patternExclude, SearchOption.TopDirectoryOnly).Select(f => new ExtendedFileInfo(f)));

                //    if (excludeFiles.Count != 0)
                //    {
                //        System.Diagnostics.Debug.WriteLine("Не подошли по паттернам файлы: " + String.Join(", ", excludeFiles.Select(f => f.FullName).ToArray()));
                //        int deleted = files.RemoveAll(delegate (ExtendedFileInfo file)
                //        {
                //            return (excludeFiles.Any(f => f.FullName == file.FullName));
                //        });
                //        System.Diagnostics.Debug.WriteLine("Удалено: " + deleted);
                //        //if (deleted != excludeFiles.Count)
                //        //    throw new Exception("Количество удаленных не равно количеству для удаления файлов");
                //    }
                //}

                //пропускаем не подходящие по размерам


                //System.Diagnostics.Debug.WriteLine(String.Format("Директория {0}, добавлено {1} файлов", di.FullName, returnFiles.Count));

                //_fileFoundCount += files.Count;
                //if (FolderChangedEvent != null)
                //    FolderChangedEvent(_fileFoundCount, di.FullName);
                //m_EvSuspend.WaitOne(); //pause

                //_directoriesSearched.Add((string)di.FullName.ToString());
                
            //}
            //catch (System.IO.FileNotFoundException)
            //{
            //    //not do
            //}
            //catch (System.UnauthorizedAccessException)
            //{
            //    //not do
            //}
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
