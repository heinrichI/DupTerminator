using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        protected async Task<ConcurrentDictionary<string, IList<ExtendedFileInfo>>> GetChecksumDictionaryAsync(ReadOnlyCollection<SearchPath> locations, IProgress<ProgressDto> progress, CancellationToken cancelToken)
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

            //var sr = await Task.WhenAll(tasksByDrivers).ContinueWith((tasks2) =>
            await Task.WhenAll(tasksByDrivers);
            //{
                foreach (var item in blockingCollectionByPhisDisks)
                {
                    item.BlockingCollection.Dispose();
                }

                //IEnumerable<DuplicateGroup>? duplicates = _checksumDictionary
                //    .Where(pair => pair.Value.Count > 1)
                //    .Select(pair => new DuplicateGroup(pair.Key, pair.Value));
                ////.OrderByDescending(d => d.Files.Any(f => f.Container is null));

                //var withoutContainer = duplicates.SelectMany(f => f.Files).Where(d => d.Container is null);
                //var d2 = duplicates.Where(d => d.Files.Any(f => withoutContainer.Any(c => f.Container is not null && c.Path == f.Container.Path)));
                //if (d2 != null && d2.Any())
                //{
                //    _logger.LogInformation($"Контейнеров с дублями: {d2.Count()}");
                //}

                //return new ReadOnlyCollection<DuplicateGroup>(duplicates.Except(d2).ToList());

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
            //});

            return _checksumDictionary;

            //return sr;
        }



        private void CalculateCheckSum(
            BlockingCollection<ExtendedFileInfo[]> blockingCollection,
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



                    if (data.All(d => d is ArchiveFileInfo))
                    {
                        foreach (var fileInfo in data)
                        {
                            string md5 = string.Empty;
                            //DateTime lastWriteTime = fileInfo is ArchiveFileInfo || fileInfo is PdfFileInfo ? fileInfo.Container.LastWriteTime : fileInfo.LastWriteTime;
                            DateTime lastWriteTime = fileInfo is ArchiveFileInfo afi && afi.ArchiveInArchive ? afi.Container.Container.LastWriteTime : fileInfo.Container.LastWriteTime;
                            if (_searchSetting.UseDB)
                            {
                                md5 = _md5Repository.ReadMD5(fileInfo.Path, lastWriteTime, fileInfo.Size);
                            }

                            if (string.IsNullOrEmpty(md5))
                            {
                                _logger.LogInformation($"Not found md5 by {fileInfo.Path}, {lastWriteTime}, {fileInfo.Size}");
                                try
                                {
                                    var checkSums = _archiveService.CalculateHashesInArchive<string>(data.Cast<ArchiveFileInfo>().ToArray(), HashHelper.CreateMD5Checksum);
                                    if (checkSums.Length != data.Length)
                                        throw new Exception("Длины не совпадают!");
                                    foreach (var checksum in checkSums)
                                    {
                                        Debug.Assert(!string.IsNullOrEmpty(checksum.Item2));
                                        _md5Repository.Add(checksum.Item1.Path, lastWriteTime, checksum.Item1.Size, checksum.Item2);
                                        _logger.LogInformation($"Save md5 by {checksum.Item1.Path}, {lastWriteTime}, {checksum.Item1.Size}");
                                        //var md52 = _md5Repository.ReadMD5(fileInfo2.Path, lastWriteTime, fileInfo2.Size);
                                        if (checksum.Item1.Path == fileInfo.Path && checksum.Item1.Size != fileInfo.Size)
                                            throw new Exception("Почему то размеры не совпадают!");


                                        _checksumDictionary.AddOrUpdate(checksum.Item2,
                                            addValueFactory: (checkSum) =>
                                            {
                                                var list = new List<ExtendedFileInfo>();
                                                list.Add(checksum.Item1);
                                                return list;
                                            },
                                            updateValueFactory: (checkSum, list) =>
                                            {
                                                list.Add(checksum.Item1);
                                                return list;
                                            });
                                    }
                                    //for (int i = 0; i < data.Length; i++)
                                    //{
                                    //    var checkSum = checkSums[i];
                                    //    var fileInfo2 = data[i];                                      
                                    //}
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"{fileInfo.Path}: {ex.Message}");
                                }
                                break;
                            }
                            else
                            {
                                _checksumDictionary.AddOrUpdate(md5,
                                        addValueFactory: (checkSum) =>
                                        {
                                            var list = new List<ExtendedFileInfo>();
                                            list.Add(fileInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checkSum, list) =>
                                        {
                                            list.Add(fileInfo);
                                            return list;
                                        });
                            }
                        }
                    }
                    else if (data.All(d => d is PdfFileInfo))
                    {
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
                                for (int i = 0; i < data.Length; i++)
                                {
                                    var checkSum = checkSums[i];
                                    var fileInfo2 = data[i];

                                    Debug.Assert(!string.IsNullOrEmpty(checkSum));
                                    _md5Repository.Add(fileInfo2.Path, lastWriteTime, fileInfo2.Size, checkSum);
                                    _checksumDictionary.AddOrUpdate(checkSum,
                                        addValueFactory: (checkSum) =>
                                        {
                                            var list = new List<ExtendedFileInfo>();
                                            list.Add(fileInfo2);
                                            return list;
                                        },
                                        updateValueFactory: (checkSum, list) =>
                                        {
                                            list.Add(fileInfo2);
                                            return list;
                                        });
                                }
                                break;
                            }
                            else
                            {
                                _checksumDictionary.AddOrUpdate(md5,
                                        addValueFactory: (checkSum) =>
                                        {
                                            var list = new List<ExtendedFileInfo>();
                                            list.Add(fileInfo);
                                            return list;
                                        },
                                        updateValueFactory: (checkSum, list) =>
                                        {
                                            list.Add(fileInfo);
                                            return list;
                                        });
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
                        }
                    }


                        //if (string.IsNullOrEmpty(md5))
                        //{
                        //    //System.Diagnostics.Debug.WriteLine(String.Format("md5 not found in DB for file {0}, lastwrite: {1}, length: {2}", _fi.FullName, _fi.LastWriteTime, _fi.Length));
                        //    if (fileInfo is ArchiveFileInfo afi)
                        //    {
                        //        fileInfo.CheckSum = _archiveService.CalculateHashInArchive<string?>(afi, HashHelper.CreateMD5Checksum);
                        //    }
                        //    else if (fileInfo is PdfFileInfo pdfInfo)
                        //    {
                        //        fileInfo.CheckSum = _pdfService.CalculateHash(pdfInfo, HashHelper.CreateMD5Checksum);
                        //    }
                        //    else
                        //    {
                        //        fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
                        //    }
                        //    Debug.Assert(!string.IsNullOrEmpty(fileInfo.CheckSum));
                        //    _md5Repository.Add(fileInfo.Path, lastWriteTime, fileInfo.Size, fileInfo.CheckSum);
                        //    //_md5Repository.Update(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
                        //}
                        //else
                        //    fileInfo.CheckSum = md5;

                        //else
                        //{
                        //    if (fileInfo is ArchiveFileInfo afi)
                        //    {
                        //        fileInfo.CheckSum = _archiveService.CalculateHashInArchive<string?>(afi, HashHelper.CreateMD5Checksum);
                        //    }
                        //    else
                        //    {
                        //        fileInfo.CheckSum = HashHelper.CreateMD5Checksum(fileInfo);
                        //    }
                        //}

                        //Debug.Assert(!string.IsNullOrEmpty(fileInfo.CheckSum));
                    //string checksum = fileInfo.CheckSum;
                    //Debug.Assert(checksum is not null);
                    //_checksumDictionary.AddOrUpdate(checksum,
                    //    addValueFactory: (checksum) =>
                    //    {
                    //        var list = new List<ExtendedFileInfo>();
                    //        list.Add(data);
                    //        return list;
                    //    },
                    //    updateValueFactory: (checksum, list) =>
                    //    {
                    //        list.Add(data);
                    //        return list;
                    //    });

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
                //foreach (ExtendedFileInfo item in group)
                //{
                //    if (cancelToken.IsCancellationRequested)
                //    {
                //        System.Diagnostics.Debug.WriteLine("CompareBySize was canceled.");
                //        break;
                //    }

                //    filesWithEqualSize.Add(item);
                //}
            }
            filesWithEqualSize.CompleteAdding();
            //var paths = filesWithEqualSize.Select(f => f.Path);
            //foreach (var item in paths)
            //{
            //    if (filesWithEqualSize.Count(f => f.Path == item) > 1)
            //        throw new Exception("Что-то не так");
            //}
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

                var fi = new FileInfo(file.Path);
                if (fi.Exists)
                {
                    var efi = new ExtendedFileInfo()
                    {
                        Size = Convert.ToUInt64(fi.Length),
                        Name = fi.Name,
                        Path = fi.FullName,
                        //LastAccessTime = fi.LastAccessTime,
                        LastWriteTime = fi.LastWriteTime,
                        DirectoryName = fi.DirectoryName,
                        Extension = fi.Extension,
                        Container = new DirectoryFileInfo { Path = Path.GetDirectoryName(file.Path) }
                    };

                    if (_archiveService.IsArchiveFile(file.Path))
                    {
                        FillInfosFromArchive(efi, files, token);
                    }
                    else if (fi.Extension.ToLower() == ".pdf")
                    {
                        FillInfosFromPdf(efi, files, token);
                    }
                    else
                    {
                        var di = new DirectoryInfo(Path.GetDirectoryName(file.Path));
                        var dFiles = di.GetFiles();

                        efi.ContainerFilesCount = dFiles.Length;
                        files.Add(efi);
                    }                     
                }
            }
            foreach (var file in locations.Where(p => !p.IsDirectory))
            {
                progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Path = file.Path, State = "Search" });

                AddFile(file, ref files, token, progress, phisicalDrive);
            }

            //progress.Report(new ProgressDto { PhisicalDrive = phisicalDrive, Status = string.Empty, State = "Search ended" });

            return new ReadOnlyCollection<ExtendedFileInfo>(files);
        }

        private void FillInfosFromArchive(ExtendedFileInfo efi, List<ExtendedFileInfo> files, CancellationToken token)
        {
            ArchiveFileInfo[] filesInArchive = null;
            if (_searchSetting.UseDB)
            {
                filesInArchive = _archiveInfoRepository.Get(efi.Path, efi.LastWriteTime, efi.Size);
                if (filesInArchive == null)
                {
                    filesInArchive = _archiveService.GetInfoFromArchive(efi, token);
                    if (filesInArchive is not null && filesInArchive.Any() && !token.IsCancellationRequested)
                    {
                        _archiveInfoRepository.Add(efi, filesInArchive);
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
                files.Add(fileArch);
            }
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
            var efi = new ExtendedFileInfo()
            {
                Size = Convert.ToUInt64(fi.Length),
                Name = fi.Name,
                Path = fi.FullName,
                //LastAccessTime = fi.LastAccessTime,
                LastWriteTime = fi.LastWriteTime,
                DirectoryName = fi.DirectoryName,
                Extension = fi.Extension
            };

            files.Add(efi);

            if (_archiveService.IsArchiveFile(efi.Path))
            {
                FillInfosFromArchive(efi, files, token);
            }
            else if (efi.Extension.ToLower() == ".pdf")
            {
                FillInfosFromPdf(efi, files, token);
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

                if (item.Container is null)
                    item.Container = new DirectoryFileInfo { Path = di.FullName };

                files.Add(item);

                if (_archiveService.IsArchiveFile(item.Path))
                {
                    FillInfosFromArchive(item, files, token);
                }
                else if (item.Extension.ToLower() == ".pdf")
                {
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
