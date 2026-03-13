using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DupTerminator.BusinessLogic
{
    public class SearcherMD5Base
    {
        private readonly SearchSetting _searchSetting;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IMd5Repository _md5Repository;
        private readonly IArchiveService _archiveService;
        private readonly IPdfService _pdfService;
        private readonly IArchiveInfoRepository _archiveInfoRepository;
        private readonly IPdfInfoRepository _pdfInfoRepository;
        private readonly ILogger _logger;


        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        protected ManualResetEventSlim _mres = new ManualResetEventSlim(true);
        private readonly ConcurrentDictionary<string, IList<ExtendedFileInfo>> _checksumDictionary = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();

        public SearcherMD5Base(
            SearchSetting searchSetting,
            IWindowsUtil windowsUtil,
            IMd5Repository md5Repository,
            IArchiveService archiveService,
            IPdfService pdfService,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            ILogger logger)
        {
            _searchSetting = searchSetting;
            _windowsUtil = windowsUtil;
            _md5Repository = md5Repository;
            _archiveService = archiveService;
            _pdfService = pdfService;
            _archiveInfoRepository = archiveInfoRepository;
            _pdfInfoRepository = pdfInfoRepository;
            _logger = logger;

            if (_md5Repository is null)
                throw new ArgumentNullException(nameof(_md5Repository));
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

        protected virtual async Task<ConcurrentDictionary<string, IList<ExtendedFileInfo>>> GetChecksumDictionaryAsync(ReadOnlyCollection<SearchPath> locations, IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            KeyValuePair<string, List<SearchPath>>[] phisicalDrives = GetPhisicalDrives(locations);

            (BlockingCollection<ExtendedFileInfo[]> BlockingCollection, string Drive)[] blockingCollectionByPhisDisks =
                new (BlockingCollection<ExtendedFileInfo[]>, string drive)[phisicalDrives.Length];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                blockingCollectionByPhisDisks[i] = new(
                    new BlockingCollection<ExtendedFileInfo[]>(), phisicalDrives[i].Key);
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
                       Path = $"{t.Result.Count} files found"
                   });
                   return t.Result; // Return the actual result
               }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            //IEnumerable<ReadOnlyCollection<ExtendedFileInfo>> results = await Task.WhenAll(tasks);
            ReadOnlyCollection<ExtendedFileInfo>[]? result = await Task.WhenAll(tasksSearch);


            //получаем список файлов
            //отсеиваем только с одинаковыми размерами
            //считаем для них хещ



            Task[] tasksByDrivers = new Task[phisicalDrives.Length * 2];
            for (int i = 0; i < blockingCollectionByPhisDisks.Length; i++)
            {
                int temp = i;
                //tasksCompareBySize[phisicalDrives.Length + temp] = Task.Factory.StartNew((d) =>
                //    CompareBySize(result[temp], blockingCollectionByPhisDisks[temp].BlockingCollection, cancelToken),
                //    cancelToken,
                //    TaskCreationOptions.LongRunning);
                tasksByDrivers[phisicalDrives.Length + temp] = Task.Run(() => CompareBySize(result[temp], blockingCollectionByPhisDisks[temp].BlockingCollection, cancelToken));
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
                tasksByDrivers[temp] = Task.Run(() => CalculateCheckSum(
                        blockingCollectionByPhisDisks[temp].BlockingCollection,
                        blockingCollectionByPhisDisks[temp].Drive, progress, cancelToken))
                     .ContinueWith(t => progress?.Report(new ProgressDto
                     {
                         PhisicalDrive = phisicalDrives[temp].Key,
                         Path = "CalculateCheckSum ended",
                         State = "CalculateCheckSum",
                         RemainSize = string.Empty,
                     }), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            await Task.WhenAll(tasksByDrivers);
            foreach (var item in blockingCollectionByPhisDisks)
            {
                item.BlockingCollection.Dispose();
            }

            return _checksumDictionary;
        }



        private void CalculateCheckSum(
            BlockingCollection<ExtendedFileInfo[]> blockingCollection,
            string drive,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            if (blockingCollection == null)
                throw new ArgumentNullException(nameof(blockingCollection));


            while (!blockingCollection.IsCompleted && !cancelToken.IsCancellationRequested)
            {
                ExtendedFileInfo[] data = null;
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
                    decimal totalSize = blockingCollection.SelectMany(c => c).Sum(b => (decimal)b.Size);
                    progress.Report(new ProgressDto
                    {
                        Path = data.First().Path,
                        State = "CalculateCheckSum",
                        PhisicalDrive = drive,
                        RemainSize = StringHelper.FormatBytes(totalSize)
                    });


                    if (data.All(d => d is ArchiveFileInfo || d is ArchiveContainer))
                    {
                        HashSet<string> added = new HashSet<string>();
                        foreach (var fileInfo in data)
                        {
                            string md5 = string.Empty;
                            //DateTime lastWriteTime = fileInfo is ArchiveFileInfo || fileInfo is PdfFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
                            DateTime lastWriteTime;
                            if (fileInfo is ArchiveContainer)
                                lastWriteTime = fileInfo.LastWriteTime;
                            else if (fileInfo is ArchiveFileInfo afi && afi.ArchiveInArchive)
                                lastWriteTime = afi.Container.Container.LastWriteTime;
                            else
                                lastWriteTime = fileInfo.Container.LastWriteTime;
                            if (_searchSetting.UseDB)
                            {
                                md5 = _md5Repository.ReadMD5(fileInfo.Path, lastWriteTime, fileInfo.Size);
                            }

                            if (string.IsNullOrEmpty(md5))
                            {
                                _logger.LogDebug($"Not found md5 for {fileInfo.Path}, {lastWriteTime}, {fileInfo.Size}");
                                try
                                {
                                    var checkSums = _archiveService.CalculateHashesInArchive<string>(data.Cast<ArchiveFileInfo>().ToArray(), HashHelper.CreateMD5Checksum);
                                    if (checkSums.Length != data.Length)
                                        throw new Exception("Длины не совпадают!");
                                    foreach (var checksum in checkSums)
                                    {
                                        Debug.Assert(!string.IsNullOrEmpty(checksum.Item2));
                                        _md5Repository.Add(checksum.Item1.Path, lastWriteTime, checksum.Item1.Size, checksum.Item2);
                                        //_logger.LogInformation($"Save md5 by {checksum.Item1.Path}, {lastWriteTime}, {checksum.Item1.Size}");
                                        //var md52 = _md5Repository.ReadMD5(fileInfo2.Path, lastWriteTime, fileInfo2.Size);
                                        if (checksum.Item1.Path == fileInfo.Path && checksum.Item1.Size != fileInfo.Size)
                                            throw new Exception("Почему то размеры не совпадают!");

                                        //файлы уже могли быть добавлены, поэтому эта проверка
                                        if (!added.Contains(checksum.Item1.Path))
                                        {
                                            AddMd5(checksum.Item1, checksum.Item2);
                                            added.Add(checksum.Item1.Path);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"{fileInfo.Path}: {ex.Message}");
                                }
                                break;
                            }
                            else
                            {
                                if (!added.Contains(fileInfo.Path))
                                {
                                    AddMd5(fileInfo, md5);
                                    added.Add(fileInfo.Path);
                                }
                            }
                        }
                    }
                    else if (data.All(d => d is PdfFileInfo))
                    {
                        HashSet<string> added = new HashSet<string>();
                        foreach (var fileInfo in data)
                        {
                            string md5 = string.Empty;
                            //DateTime lastWriteTime = fileInfo is ArchiveFileInfo || fileInfo is PdfFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
                            DateTime lastWriteTime = fileInfo.Container.LastWriteTime;
                            if (_searchSetting.UseDB)
                            {
                                md5 = _md5Repository.ReadMD5(fileInfo.Path, lastWriteTime, fileInfo.Size);
                            }

                            if (string.IsNullOrEmpty(md5))
                            {
                                var checkSums = _pdfService.CalculateHashes(data.Cast<PdfFileInfo>().ToArray(), HashHelper.CreateMD5Checksum);
                                if (checkSums.Length != data.Length)
                                    throw new Exception("Длины не совпадают!");
                                foreach (var checksum in checkSums)
                                {
                                    Debug.Assert(!string.IsNullOrEmpty(checksum.Item2));
                                    _md5Repository.Add(checksum.Item1.Path, lastWriteTime, checksum.Item1.Size, checksum.Item2);
                                    //файлы уже могли быть добавлены, поэтому эта проверка
                                    if (!added.Contains(checksum.Item1.Path))
                                    {
                                        AddMd5(checksum.Item1, checksum.Item2);
                                        added.Add(checksum.Item1.Path);
                                    }
                                }
                                break;
                            }
                            else
                            {
                                if (!added.Contains(fileInfo.Path))
                                {
                                    AddMd5(fileInfo, md5);
                                    added.Add(fileInfo.Path);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var fileInfo in data)
                        {
                            string md5 = string.Empty;
                            DateTime lastWriteTime = fileInfo.LastWriteTime;
                            if (_searchSetting.UseDB)
                            {
                                md5 = _md5Repository.ReadMD5(fileInfo.Path, lastWriteTime, fileInfo.Size);
                            }
                            if (string.IsNullOrEmpty(md5))
                            {
                                md5 = HashHelper.CreateMD5Checksum(fileInfo);
                            }
                            AddMd5(fileInfo, md5);
                        }
                    }
                }
            }
        }

        private void AddMd5(ExtendedFileInfo fileInfo, string md5)
        {
            Debug.Assert(fileInfo != null);
            _checksumDictionary.AddOrUpdate(md5,
                addValueFactory: (checksum) =>
                {
                    var list = new List<ExtendedFileInfo>();
                    list.Add(fileInfo);
                    return list;
                },
                updateValueFactory: (checksum, list) =>
                {
                    list.Add(fileInfo);
                    return list;
                });

            Debug.Assert(!(_checksumDictionary[md5].Count > 1 && _checksumDictionary[md5].All(f => f.Path == _checksumDictionary[md5].First().Path)));
        }

        protected static void CompareBySize(
            ReadOnlyCollection<ExtendedFileInfo> foundedFiles,
            BlockingCollection<ExtendedFileInfo[]> filesWithEqualSize,
            CancellationToken cancelToken)
        {
            var groups = foundedFiles.GroupBy(fi => fi.Size)
                .Where(group => group.Count() > 1)
                .SelectMany(g => g)
                .GroupBy(gg => gg is ArchiveFileInfo agg && agg.ArchiveInArchive ? gg.Container.Container : gg.Container).ToArray();
            foreach (var group in groups)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("CompareBySize was canceled.");
                    break;
                }

                filesWithEqualSize.Add(group.ToArray());
            }
            filesWithEqualSize.CompleteAdding();
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
                    _logger.LogInformation("SearchFileOnPhisicalDrive was canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = directory.Path, State = "Search" });

                DirectoryInfo di = new System.IO.DirectoryInfo(directory.Path);
                AddFilesFromDirectory(di, ref files, directory.SearchInSubFolder, token, progress, phisicalDrive);
            }

            foreach (var file in locations.Where(p => !p.IsDirectory))
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogInformation("SearchFileOnPhisicalDrive was canceled.");
                    break;
                }

                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = file.Path, State = "Search" });

                AddFile(file, ref files, token, phisicalDrive);
            }

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }

        private ArchiveContainer FillInfosFromArchive(ExtendedFileInfo efi, List<ExtendedFileInfo> files, CancellationToken token)
        {
            ArchiveFileInfo[] filesInArchive = null;
            if (_searchSetting.UseDB)
            {
                filesInArchive = _archiveInfoRepository.Get(efi.Path, efi.LastWriteTime, efi.Size);
                if (filesInArchive == null)
                {
                    _logger.LogDebug($"{efi.Path} не найден в _archiveInfoRepository");
                    filesInArchive = _archiveService.GetInfoFromArchive(efi, token);
                    if (filesInArchive is not null && filesInArchive.Any() && !token.IsCancellationRequested)
                    {
                        _archiveInfoRepository.Add(efi, filesInArchive);
//#if DEBUG
//                        var fromDb = _archiveInfoRepository.Get(efi.Path, efi.LastWriteTime, efi.Size);
//                        Debug.Assert(fromDb.Length == filesInArchive.Length);
//                        for (int i = 0; i < fromDb.Length; i++)
//                        {
//                            Debug.Assert(fromDb[i].Name == filesInArchive[i].Name);
//                            Debug.Assert(fromDb[i].Path == filesInArchive[i].Path);
//                            Debug.Assert(fromDb[i].Size == filesInArchive[i].Size);
//                            Debug.Assert(fromDb[i].LastWriteTime == filesInArchive[i].LastWriteTime);
//                            Debug.Assert(fromDb[i].Extension == filesInArchive[i].Extension);
//                            Debug.Assert(fromDb[i].ArchiveCRC == filesInArchive[i].ArchiveCRC);
//                            Debug.Assert(fromDb[i].ArchiveExtension == filesInArchive[i].ArchiveExtension);
//                            Debug.Assert(fromDb[i].ArchiveFileName == filesInArchive[i].ArchiveFileName);
//                            Debug.Assert(fromDb[i].ArchiveInArchive == filesInArchive[i].ArchiveInArchive);
//                            Debug.Assert(fromDb[i].ArchivePath == filesInArchive[i].ArchivePath);
//                            Debug.Assert(fromDb[i].Container.Path == filesInArchive[i].Container.Path);
//                            Debug.Assert(fromDb[i].Container.Name == filesInArchive[i].Container.Name);
//                            Debug.Assert(fromDb[i].Container.Size == filesInArchive[i].Container.Size);
//                            Debug.Assert(fromDb[i].Container.LastWriteTime == filesInArchive[i].Container.LastWriteTime);
//                            Debug.Assert(fromDb[i].ContainerFilesCount == filesInArchive[i].ContainerFilesCount);
//                            Debug.Assert(fromDb[i].DirectoryName == filesInArchive[i].DirectoryName);
//                            if (filesInArchive[i].ArchiveInArchive)
//                            {
//                                Debug.Assert(fromDb[i].Container.Container.Path == filesInArchive[i].Container.Container.Path);
//                                Debug.Assert(fromDb[i].Container.Container.Name == filesInArchive[i].Container.Container.Name);
//                                Debug.Assert(fromDb[i].Container.Container.Size == filesInArchive[i].Container.Container.Size);
//                                Debug.Assert(fromDb[i].Container.Container.LastWriteTime == filesInArchive[i].Container.Container.LastWriteTime);
//                            }
//                        }
//#endif
                    }
                }
                if (filesInArchive.Any() && string.IsNullOrEmpty(filesInArchive.FirstOrDefault().Name))
                    throw new Exception("_archiveInfoRepository return empty!");
            }
            else
            {
                filesInArchive = _archiveService.GetInfoFromArchive(efi, token);
            }
            foreach (ExtendedFileInfo fileArch in filesInArchive)
            {
                if (_searchSetting.IncludePattern.Any())
                {
                    if (_searchSetting.IncludePattern.Contains(fileArch.Extension))
                        AddFileToList(files, fileArch);
                }
                else
                    AddFileToList(files, fileArch);
                Debug.Assert(filesInArchive.Count(b => b.Path == fileArch.Path) == 1);
            }
            return new ArchiveContainer(efi)
            {
                Files = filesInArchive.Select(c => new ArchiveSimpleFileInfo(c)).ToArray(),
                //FilesCount = filesInArchive.Length
            };
        }

        private void AddFileToList(List<ExtendedFileInfo> files, ExtendedFileInfo file)
        {
            if (file.Name == "Thumbs.db" && file.Extension == ".db")
            {
                _logger.LogWarning($"Thumbs.db in {file.Path}");
                Debug.WriteLine($"Thumbs.db in {file.Path}");
            }
            files.Add(file);
        }

        private void AddFile(SearchPath file, ref List<ExtendedFileInfo> files, CancellationToken token, string phisicalDrive)
        {
            var fi = new FileInfo(file.Path);
            var efi = new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(fi.Length),
                Name = fi.Name,
                Path = fi.FullName,
                LastWriteTime = fi.LastWriteTime,
                DirectoryName = fi.DirectoryName,
                Extension = fi.Extension,
            };

            //IsArchiveFile is slow
            if ((_searchSetting.UseDB && _archiveInfoRepository.Exist(efi.Path, efi.LastWriteTime, efi.Size)) || _archiveService.IsArchiveFile(efi.Path))
            {
                ArchiveContainer afi = FillInfosFromArchive(efi, files, token);
                files.Add(afi);
            }
            else if (efi.Extension.ToLower() == ".pdf")
            {
                if (!_searchSetting.IncludePattern.Any())
                    FillInfosFromPdf(efi, files, token);
            }
            else
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(file.Path));
                var dFiles = di.GetFiles();

                efi.Container = new DirectoryContainer
                {
                    Path = di.FullName,
                    //FilesCount = dFiles.Length
                    Files = dFiles.Select(f => new SimpleFileInfo
                    {
                        Name = f.Name,
                        Path = f.FullName,
                        Size = Convert.ToUInt64(f.Length),
                    }).ToArray()
                };                
                files.Add(efi);
            }
        }

        private void FillInfosFromPdf(ExtendedFileInfo efi, List<ExtendedFileInfo> files, CancellationToken token)
        {
            IEnumerable<PdfFileInfo> filesInPdf;
            if (_searchSetting.UseDB)
            {
                filesInPdf = _pdfInfoRepository.Get(efi.Path, efi.LastWriteTime, efi.Size);
                if (filesInPdf == null)
                {
                    _logger.LogDebug($"{efi.Path} не найден в _pdfInfoRepository");
                    filesInPdf = _pdfService.GetInfos(efi, token);
                    if (filesInPdf is not null && filesInPdf.Any() && !token.IsCancellationRequested)
                    {
                        _pdfInfoRepository.Add(efi, filesInPdf);
                    }
                }
            }
            else
            {
                filesInPdf = _pdfService.GetInfos(efi, token);
            }
            foreach (var pdfInfo in filesInPdf)
            {
                files.Add(pdfInfo);
            }
        }

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

            decimal totalSize = files3.Sum(b => (decimal)b.Size);
            decimal remainSize = totalSize;
            foreach (var item in files3)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogInformation("AddFiles canceled.");
                    break;
                }

                progress.Report(new ProgressDto
                {
                    PhisicalDrive = phisicalDrive,
                    Path = item.Path,
                    State = "Search",
                    RemainSize = StringHelper.FormatBytes(remainSize)
                });
                remainSize -= item.Size;

                if (_searchSetting.IncludePattern.Any())
                {
                    if (_searchSetting.IncludePattern.Contains(item.Extension))
                        AddFileToList(files, item);
                }
                else
                    AddFileToList(files, item);

                if ((_searchSetting.UseDB && _archiveInfoRepository.Exist(item.Path, item.LastWriteTime, item.Size)) || _archiveService.IsArchiveFile(item.Path))
                {
                    _ = FillInfosFromArchive(item, files, token);
                }
                else if (item.Extension.ToLower() == ".pdf")
                {
                    if (!_searchSetting.IncludePattern.Any())
                        FillInfosFromPdf(item, files, token);
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

    }
}
