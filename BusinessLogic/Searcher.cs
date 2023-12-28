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
using DupTerminator.BusinessLogic.Helper;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DupTerminator.BusinessLogic
{
    public class Searcher : IDisposable
    {
        private readonly ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> _directorySearchCollection;
        private readonly ReadOnlyCollection<string> _fileSearch;
        private readonly SearchSetting _searchSetting;
        private readonly IDBManager _dbManager;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IProgress<ProgressDto> _progress;
        private readonly IArchiveService _archiveService;
        private readonly ILogger<Searcher> _logger;
        private readonly ConcurrentDictionary<string, IList<ExtendedFileInfo>> _checksumDictionary = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
        public ReadOnlyCollection<DuplicateGroup> Duplicates { get; private set; }

        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);

        private CancellationTokenSource _cts;

        //IProgress<Tuple<int, string>> _progressSearchFile = new Progress<Tuple<int, string>>();

        //IProgress<Tuple<int, string>> _progressCalculateDuplicate = new Progress<Tuple<int, string>>();

        public Searcher(
            ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> directorySearchCollection,
            ReadOnlyCollection<string> fileSearch,
            SearchSetting searchSetting,
            IDBManager dbManager,
            IWindowsUtil windowsUtil,
            IProgress<ProgressDto> progress,
            IArchiveService archiveService,
            ILogger<Searcher> logger)
        {
            _directorySearchCollection = directorySearchCollection;
            _fileSearch = fileSearch;
            _searchSetting = searchSetting;
            _dbManager = dbManager;
            _windowsUtil = windowsUtil;
            _progress = progress;
            _archiveService = archiveService;
            _logger = logger;
        }

        public async Task Start()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            //var progress = new Progress<int>(value => { progressBar.Value = value; });
            //await Task.Run(() => GenerateAsync(progress));
            //curent found files, current file path


            ReadOnlyCollection<string> phisicalDrives = GetPhisicalDrives(_directorySearchCollection, _fileSearch);

            BlockingCollection<ExtendedFileInfo>[]? blockingCollectionByPhisDisks = new BlockingCollection<ExtendedFileInfo>[phisicalDrives.Count];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                blockingCollectionByPhisDisks[i] = new BlockingCollection<ExtendedFileInfo>();
            }

            var tasks = new Task<ReadOnlyCollection<ExtendedFileInfo>>[phisicalDrives.Count];
            for (int i = 0; i < phisicalDrives.Count; i++)
            {
                int temp = i;
                tasks[i] = Task.Factory.StartNew<ReadOnlyCollection<ExtendedFileInfo>>(
                    () => SearchFileOnPhisicalDrive(_progress, phisicalDrives[temp], _cts.Token),
                    _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            };

            //IEnumerable<ReadOnlyCollection<ExtendedFileInfo>> results = await Task.WhenAll(tasks);
            ReadOnlyCollection<ExtendedFileInfo>[]? result = await Task.WhenAll(tasks);

            //получаем список файлов
            //отсеиваем только с одинаковыми размерами
            //считаем для них хещ


            //Task.WhenAll(tasks).ContinueWith((files) =>
            //{
            //var collections = new ConcurrentDictionary<string, BlockingCollection<string>>();

            //try
            //{

            Task[] tasks2 = new Task[phisicalDrives.Count * 2];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                int temp = i;
                tasks2[phisicalDrives.Count + temp] = Task.Factory.StartNew((d) =>
                    CompareBySize(result[temp], blockingCollectionByPhisDisks[temp]),
                    _cts, TaskCreationOptions.LongRunning);
            }

            for (int i = 0; i < phisicalDrives.Count; i++)
            {
                int temp = i;
                tasks2[temp] = Task.Factory.StartNew(
                    () => CalculateCheckSum(blockingCollectionByPhisDisks[temp], _progress),
                    _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }

            await Task.WhenAll(tasks2).ContinueWith((tasks2) =>
            {
                foreach (var item in blockingCollectionByPhisDisks)
                {
                    item.Dispose();
                }
                var duplicates = _checksumDictionary
                    .Where(pair => pair.Value.Count > 1)
                    .Select(pair => new DuplicateGroup(pair));

                //Duplicates = new ReadOnlyCollection<DuplicateGroup>(duplicates.ToList());

                var duplicatesDict = duplicates.ToDictionary(k => k.Checksum);

                //если все файлы из одного контейнера совпадают, то удаляем их и оставляем только контейнер
                var containers = duplicates.SelectMany(d => d.Files).GroupBy(f => f.Container)
                    .Select(g => new Pair<ExtendedFileInfo, IList<ExtendedFileInfo>>(g.Key, g.ToList()))
                    .ToArray();
                foreach (Pair<ExtendedFileInfo, IList<ExtendedFileInfo>>? container in containers)
                {
                    if (container.Key is null)
                        continue;

                    ExtendedFileInfo? firstFile = container.Value.FirstOrDefault();
                    if (firstFile != null)
                    {
                        DuplicateGroup group = duplicatesDict[firstFile.CheckSum];
                        //если файлы лежат не только в контейнере, проверить не совпадают ли все файлы из контейнера с файлами в директории, если совпадают - создать виртуальный контейнер
                        var duplCandidates = group.Files.Where(f => f.Container?.CombinedPath != container?.Key?.CombinedPath).GroupBy(f => f.Container);
                        foreach (IGrouping<ExtendedFileInfo, ExtendedFileInfo> duplCandidate in duplCandidates)
                        {
                            if (duplCandidate.Key is null)
                            {
                                //этот файл лежит просто в директории
                                var filesInDirectory = duplicates.SelectMany(d => d.Files).Where(f => f.DirectoryName == duplCandidate.First().DirectoryName);
                                foreach (var file in container.Value)
                                {
                                    if (!filesInDirectory.Any(f => f.Size == file.Size && f.CheckSum == f.CheckSum))
                                        break;
                                }
                                добавить виртуальный контейнер
                            }
                            else
                            {
                                Pair<ExtendedFileInfo, IList<ExtendedFileInfo>> duplicateContainer = containers.Single(c => c.Key != null && c.Key.CombinedPath == duplCandidate.Key?.CombinedPath);
                                if (duplicateContainer.Value.Count == container.Value.Count)
                                {
                                    bool sequenceEqual = duplicateContainer.Value.SequenceEqual(container.Value, new CheckSumComparer());
                                    if (sequenceEqual)
                                    {
                                        container.Value.Clear();
                                        duplicateContainer.Value.Clear();
                                        //foreach (var item in duplicates)
                                        //{
                                        //if (item.Files.First().Container.CombinedPath == can.Key.CombinedPath || item.Files.First().Container.CombinedPath == container.Key.CombinedPath)
                                        //{
                                        //    item.Files.RemoveAll(f => f.CombinedPath == can.Key.CombinedPath && can.Any(c => c.Name == f.Name));
                                        //    item.Files.RemoveAll(f => f.CombinedPath == container.Key.CombinedPath && container.Any(c => c.Name == f.Name));
                                        //}
                                        //}
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug($"У кандитаного контненера {duplCandidate} не совпадает количество файлов");
                                }
                            }
                        }
                    }
                }

                Duplicates = new ReadOnlyCollection<DuplicateGroup>(containers
                    .SelectMany(c => c.Value)
                    .GroupBy(f => f.CheckSum)
                    .Select(f => new DuplicateGroup(f))
                    .ToList());
            });
        }

        private ReadOnlyCollection<string> GetPhisicalDrives(
            ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> directorySearchCollection,
            ReadOnlyCollection<string> fileSearch)
        {
            List<string>? driveLetters = new List<string>();
            if (directorySearchCollection != null)
                driveLetters.AddRange(directorySearchCollection.Select(directory => directory.DirectoryPath.Substring(0, 2).ToLowerInvariant()).Distinct());
            if (fileSearch != null)
                driveLetters.AddRange(fileSearch.Select(file => file.Substring(0, 2).ToLowerInvariant()).Distinct());

            HashSet<string> models = new HashSet<string>();
            foreach (var drive in driveLetters)
            {
                var model = _windowsUtil.GetModelFromDrive(drive);
                if (model != null)
                    models.Add(model);
            }
            return new ReadOnlyCollection<string>(models.ToList());
        }

        private void CompareBySize(ReadOnlyCollection<ExtendedFileInfo> foundedFiles, BlockingCollection<ExtendedFileInfo> filesWithEqualSize)
        {
            IEnumerable<IGrouping<ulong, ExtendedFileInfo>>? groupsFilesWithEqualSize = foundedFiles.GroupBy(fi => fi.Size).Where(group => group.Count() > 1);
            foreach (IGrouping<ulong, ExtendedFileInfo>? items in groupsFilesWithEqualSize)
            {
                foreach (ExtendedFileInfo item in items)
                {
                    filesWithEqualSize.Add(item);
                }
            }
            filesWithEqualSize.CompleteAdding();
        }

        // из разных потоков
        private ReadOnlyCollection<ExtendedFileInfo> SearchFileOnPhisicalDrive(
            IProgress<ProgressDto> progress,
            in string phisicalDrive,
            CancellationToken token)
        {
            List<ExtendedFileInfo> files = new List<ExtendedFileInfo>();
            foreach (var directory in _directorySearchCollection)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = directory.DirectoryPath });

                DirectoryInfo di = new System.IO.DirectoryInfo(directory.DirectoryPath);
                AddFiles(di, ref files, directory.SearchInSubdirectory, token, progress, phisicalDrive);
            }

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }

        private void CalculateCheckSum(BlockingCollection<ExtendedFileInfo> blockingCollection, IProgress<ProgressDto> progress)
        {
            if (blockingCollection == null)
                throw new ArgumentNullException(nameof(blockingCollection));

            //var timeout = TimeSpan.FromMilliseconds(1000);
            //int localSum = 0;
            //while (bc.TryTake(out ExtendedFileInfo localItem, timeout))
            //{
            //    localSum++;
            //}

            while (!blockingCollection.IsCompleted)
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
                    progress.Report(new ProgressDto { Status = data.Name });

                    string checksum = GetCheckSum(data);
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
                    //string md5 = string.Empty;
                    //md5 = _dbManager.ReadMD5(data.FullName, data.LastWriteTime, data.Length);
                    //if (String.IsNullOrEmpty(md5))
                    //{
                    //    //System.Diagnostics.Debug.WriteLine(String.Format("md5 not found in DB for file {0}, lastwrite: {1}, length: {2}", _fi.FullName, _fi.LastWriteTime, _fi.Length));
                    //    data.CheckSum = CreateMD5Checksum(_fileInfo.FullName);
                    //    dbManager.Add(_fileInfo.FullName, _fileInfo.LastWriteTime, _fileInfo.Length, _checkSum);
                    //    //_dbManager.Update(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
                    //}
                    //else
                    //    data.CheckSum = md5;
                }
                else
                {
                    if (fileInfo.InArchive)
                    {
                        Debug.Assert(fileInfo.Container != null);
                        fileInfo.CheckSum = _archiveService.CalculateHashInArchive(fileInfo);
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
            try
            {
                //Add subdirectories
                if (isRecurse)
                {
                    var directories = di.GetDirectories();
                    for (int i = 0; i < directories.Length; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("Canceled while running.");
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

                        progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = directories[i].FullName });

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
                    DirectoryName = f.DirectoryName,
                    Extension = f.Extension,
                });
                foreach (var item in files3)
                {
                    if (token.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("Canceled while running.");
                        break;
                    }

                    progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = item.Path });

                    files.Add(item);
                    if (_archiveService.IsArchiveFile(item.Path))
                    {
                        var files4 = _archiveService.GetInfoFromArchive(item.Path, item, token);
                        foreach (ExtendedFileInfo file in files4)
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
                
            }
            catch (System.IO.FileNotFoundException)
            {
                //not do
            }
            catch (System.UnauthorizedAccessException)
            {
                //not do
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }

        public void Cancell()
        {
            // Token can only be canceled once.
            _cts.Cancel();
        }

        public void Pause()
        {
            _mres.Reset();
        }

        public void Resume()
        {
            _mres.Set();
        }

        private class CheckSumComparer : IEqualityComparer<ExtendedFileInfo>
        {
            public bool Equals(ExtendedFileInfo x, ExtendedFileInfo y)
            {
                //Check whether the compared objects reference the same data.
                if (object.ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
                    return false;

                return x.CheckSum == y.CheckSum;
            }

            // If Equals() returns true for a pair of objects
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode([DisallowNull] ExtendedFileInfo obj)
            {
                //Check whether the object is null
                if (object.ReferenceEquals(obj, null)) return 0;

                int hash = obj.CheckSum == null ? 0 : obj.CheckSum.GetHashCode();

                return hash;
            }
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
