using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class Searcher : IDisposable
    {
        private readonly ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> _directorySearchCollection;
        private readonly ReadOnlyCollection<string> _fileSearch;
        private readonly IDBManager _dbManager;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IProgress<ProgressDto> _progress;
        private readonly IArchiveService _archiveService;
        private readonly ConcurrentDictionary<string, List<ExtendedFileInfo>> _checksumDictionary = new ConcurrentDictionary<string, List<ExtendedFileInfo>>();
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
            IArchiveService archiveService)
        {
            _directorySearchCollection = directorySearchCollection;
            _fileSearch = fileSearch;
            _dbManager = dbManager;
            _windowsUtil = windowsUtil;
            _progress = progress;
            _archiveService = archiveService;
        }

        public async Task Start()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            //var progress = new Progress<int>(value => { progressBar.Value = value; });
            //await Task.Run(() => GenerateAsync(progress));
            //curent found files, current file path


            var phisicalDrives = GetPhisicalDrives(_directorySearchCollection, _fileSearch);


            var tasks = new Task<ReadOnlyCollection<ExtendedFileInfo>>[phisicalDrives.Count];
            for (int i = 0; i < phisicalDrives.Count; i++)
            {
                int temp = i;
                tasks[i] = Task.Factory.StartNew<ReadOnlyCollection<ExtendedFileInfo>>(
                    () => SearchFileOnPhisicalDrive(_progress, _cts.Token, phisicalDrives[temp]),
                    _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            };

            //IEnumerable<ReadOnlyCollection<ExtendedFileInfo>> results = await Task.WhenAll(tasks);
            ReadOnlyCollection<ExtendedFileInfo>[]? result = await Task.WhenAll(tasks);


            //Task.WhenAll(tasks).ContinueWith((files) =>
            //{
            //var collections = new ConcurrentDictionary<string, BlockingCollection<string>>();
            BlockingCollection<ExtendedFileInfo>[]? blockingCollectionByPhisDisks = new BlockingCollection<ExtendedFileInfo>[phisicalDrives.Count];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                blockingCollectionByPhisDisks[i] = new BlockingCollection<ExtendedFileInfo>();
            }
            //try
            //{
            var tasks2 = new Task[phisicalDrives.Count * 2];
            for (int i = 0; i < phisicalDrives.Count; i++)
            {
                int temp = i;
                tasks2[i] = Task.Factory.StartNew(
                    () => CalculateCheckSum(blockingCollectionByPhisDisks[temp], _progress),
                    _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                int temp = i;
                tasks2[phisicalDrives.Count + temp] = Task.Factory.StartNew((d) => CompareBySize(result[temp],
                    blockingCollectionByPhisDisks[temp]),
                    _cts, TaskCreationOptions.LongRunning);
            }

            await Task.WhenAll(tasks2).ContinueWith((tasks2) =>
            {
                foreach (var item in blockingCollectionByPhisDisks)
                {
                    item.Dispose();
                }
                Duplicates = new ReadOnlyCollection<DuplicateGroup>(_checksumDictionary
                    .Where(pair => pair.Value.Count > 1)
                    .Select(pair => new DuplicateGroup(pair))
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

        private ReadOnlyCollection<ExtendedFileInfo> SearchFileOnPhisicalDrive(
            IProgress<ProgressDto> progress,
            CancellationToken token,
            in string phisicalDrive)
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

        private void CalculateCheckSum(BlockingCollection<ExtendedFileInfo> blockingCollection, object progress2)
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
                data = blockingCollection.Take();

                if (data != null)
                {
                    string checksum = data.GetCheckSum(_dbManager);
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

                    _archiveService.IsArchiveFile(data.FullName)
                    if (ArchiveFile.IsArchive(data.FullName))
                    {
                        using (ArchiveFile archiveFile = new ArchiveFile(data.FullName))
                        {
                            foreach (var entry in archiveFile.Entries)
                            {
                                //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
                                if (entry.IsFolder)
                                {
                                    continue;
                                }

                                using (MemoryStream entryMemoryStream = new MemoryStream())
                                {
                                    entry.Extract(entryMemoryStream);

                                    string checksumInArchive = entryMemoryStream.ToArray().MD5String();

                                    ExtendedFileInfo archiveInfo = new ExtendedFileInfo(data, entry);

                                    _checksumDictionary.AddOrUpdate(checksumInArchive,
                                        addValueFactory: (checksum) =>
                                        {
                                            var list = new List<ExtendedFileInfo>();
                                            list.Add(archiveInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checksum, list) =>
                                        {
                                            list.Add(archiveInfo);
                                            return list;
                                        });
                                }
                            }
                        }
                    }
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
                files.AddRange(di.GetFiles("*", SearchOption.TopDirectoryOnly).Select(fi => new ExtendedFileInfo(fi)));

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

                //расширения
                /*if (_includePattern.Count > 0)
                {
                    foreach (string pattern in _includePattern)
                        files.AddRange(di.GetFiles(pattern, SearchOption.TopDirectoryOnly));
                }
                else
                    files.AddRange(di.GetFiles());

                if (_excludePattern.Count > 0)
                {
                    List<FileInfo> excludeFiles = new List<FileInfo>();

                    foreach (string patternExclude in _excludePattern)
                        excludeFiles.AddRange(di.GetFiles(patternExclude, SearchOption.TopDirectoryOnly));

                    if (excludeFiles.Count != 0)
                    {
                        Debug.WriteLine("Не подошли по паттернам файлы: " + String.Join(", ", excludeFiles.Select(f => f.FullName).ToArray()));
                        int deleted = files.RemoveAll(delegate (FileInfo file)
                        {
                            return (excludeFiles.Any(f => f.FullName == file.FullName));
                        });
                        Debug.WriteLine("Удалено: " + deleted);
                        //if (deleted != excludeFiles.Count)
                        //    throw new Exception("Количество удаленных не равно количеству для удаления файлов");
                    }
                }

                //пропускаем не подходящие по размерам
                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i].Length > settings.Fields.limits[0] && files[i].Length < settings.Fields.limits[1])
                    {
                        returnFiles.Add(new ExtendedFileInfo(files[i]));

                        if (settings.Fields.IsScanMax)
                            if (returnFiles.Count >= settings.Fields.MaxFile)
                            {
                                Debug.WriteLine("Слишком много файлов = " + returnFiles.Count);
                                return;
                            }
                    }
                }

                Debug.WriteLine(String.Format("Директория {0}, добавлено {1} файлов", di.FullName, returnFiles.Count));

                _fileFoundCount += files.Count;
                if (FolderChangedEvent != null)
                    FolderChangedEvent(_fileFoundCount, di.FullName);
                //m_EvSuspend.WaitOne(); //pause

                //_directoriesSearched.Add((string)di.FullName.ToString());
                */
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
    }
}
