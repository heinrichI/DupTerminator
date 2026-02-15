using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DupTerminator.BusinessLogic
{
    public class SearcherMD5Container : SearcherMD5Base
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly MD5ContainerSettings _modeSettings;

        private readonly ILogger<SearcherMD5> _logger;
        //public ReadOnlyCollection<DuplicateGroup> Duplicates { get; private set; }

        // New-style MRESlim that supports unified cancellation
        // in its Wait methods.
        ManualResetEventSlim _mres = new ManualResetEventSlim(true);
        private readonly Stopwatch _stopwatch = new();

        public SearcherMD5Container(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            MD5ContainerSettings modeSettings,
            IMd5Repository md5Repository,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            IWindowsUtil windowsUtil,
            //IProgress<ProgressDto> progress,
            //CancellationToken cancellationToken,
            IArchiveService archiveService,
            IPdfService pdfService,
            ILogger<SearcherMD5> logger) : base(searchSetting, windowsUtil, md5Repository, archiveService, pdfService, archiveInfoRepository, pdfInfoRepository, logger)
        {
            _locations = locations;
            _modeSettings = modeSettings;
            _logger = logger;
        }

        public async Task<ReadOnlyCollection<DuplicateContainer>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            _stopwatch.Restart();

            ConcurrentDictionary<string, IList<ExtendedFileInfo>> checksumDictionary = await GetChecksumDictionaryAsync(_locations, progress, cancelToken);

            _stopwatch.Stop();
            _logger.LogInformation($"ElapsedTime: {_stopwatch.Elapsed}");
            Debug.WriteLine($"ElapsedTime: {_stopwatch.Elapsed}");

            //var paths = checksumDictionary.SelectMany(f => f.Value.Select(h => h.Path)).ToArray();
            //foreach (var path in paths)
            //{
            //    if (paths.Count(f => f == path) > 1)
            //        throw new Exception("Что-то не так");
            //}
            //var s = JsonSerializer.Serialize(checksumDictionary, new JsonSerializerOptions
            //{
            //    // This allows all Unicode characters to remain unescaped
            //    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            //    WriteIndented = true
            //});

            Dictionary<(ContainerEqInfo, ContainerEqInfo), ContainerInfo> containers = new Dictionary<(ContainerEqInfo, ContainerEqInfo), ContainerInfo>();
            foreach (KeyValuePair<string, IList<ExtendedFileInfo>> pair in checksumDictionary)
            {
                if (pair.Value.Count > 1)
                {
                    // Get the list of ExtendedFileInfo for this checksum
                    IList<ExtendedFileInfo> files = pair.Value;

                    // Iterate through all pairs (i, j) where i < j
                    for (int i = 0; i < files.Count; i++)
                    {
                        for (int j = i + 1; j < files.Count; j++)
                        {
                            ExtendedFileInfo first = files[i];
                            ExtendedFileInfo second = files[j];

                            if (first.Container.Equals(second.Container))
                                continue;

                            // Ensure the pair is ordered by path (to avoid duplicates)
                            if (string.Compare(first.Container.Path, second.Container.Path, StringComparison.Ordinal) > 0)
                            {
                                // Swap if first.Path > second.Path
                                (first, second) = (second, first);
                            }

                            //если это сами контейнеры
                            var key = (new ContainerEqInfo(first), new ContainerEqInfo(second));
                            if (containers.ContainsKey(key))
                            {
                                containers[key].TheyThemselvesAreEqual = true;
                                containers[key].FirstFiles.Clear();
                                containers[key].SecondFiles.Clear();
                            }


                            var containersKey = (new ContainerEqInfo(first.Container, first.ContainerFilesCount, first), new ContainerEqInfo(second.Container, second.ContainerFilesCount, second));

                            // Initialize the list if the key doesn't exist
                            if (!containers.TryGetValue(containersKey, out ContainerInfo value))
                            {
                                value = new ContainerInfo();
                                containers[containersKey] = value;
                            }

                            if (value.TheyThemselvesAreEqual)
                                continue;

                            // Add the current checksum's file list (or just the checksum? Clarify your intent)
                            // Since the value is List<ExtendedFileInfo>, you might want to add all files from the current checksum group?
                            // Alternatively, if you want to aggregate which checksum groups contribute to this container pair,
                            // you might add the entire list (pair.Value) or just the current file pair?
                            // Based on your structure, it seems you want to collect all ExtendedFileInfo from checksum groups that have this container pair.
                            // But note: this might add duplicates if the same file appears in multiple checksum groups?
                            // Clarification needed: What exactly should be in the List<ExtendedFileInfo> value?

                            // Assuming you want to add all files from the current checksum group (pair.Value)
                            //value.AddRange([first, second]);
                            if (!value.FirstFiles.Contains(first))
                            {
                                value.FirstFiles.Add(first);
                                value.SecondFiles.Add(second);
                            }
                        }
                    }
                }
            }

            //var forRemove = containers.Values.SelectMany(v =>
            //{
            //    List<(ContainerEqInfo, ContainerEqInfo)> list = new List<(ContainerEqInfo, ContainerEqInfo)>();
            //    if (!v.TheyThemselvesAreEqual)
            //    {
            //        list.Add((new ContainerEqInfo(v.FirstFiles.First()), new ContainerEqInfo(v.SecondFiles.First())));
            //    }
            //    else
            //    {
            //        for (int i = 0; i < v.FirstFiles.Count; i++)
            //        {
            //            var first = v.FirstFiles[i];
            //            var second = v.SecondFiles[i];

            //            if (string.Compare(first.Path, second.Path, StringComparison.Ordinal) > 0)
            //            {
            //                // Swap if first.Path > second.Path
            //                (first, second) = (second, first);
            //            }

            //            list.Add((new ContainerEqInfo(first), new ContainerEqInfo(second)));
            //        }
            //    }
            //    return list;
            //}).ToArray();
            //foreach (var item in forRemove)
            //{
            //    containers.Remove(item);
            //}



            //IEnumerable<DuplicateGroup>? duplicates = _checksumDictionary
            //    .Where(pair => pair.Value.Count > 1)
            //    .Select(pair => new DuplicateGroup(pair.Key, pair.Value));


            //var withoutContainer = duplicates.SelectMany(f => f.Files).Where(d => d.Container is null);
            //var d2 = duplicates.Where(d => d.Files.Any(f => withoutContainer.Any(c => f.Container is not null && c.Path == f.Container.Path)));
            //if (d2 != null && d2.Any())
            //{
            //    _logger.LogInformation($"Контейнеров с дублями: {d2.Count()}");
            //}

            //return new ReadOnlyCollection<DuplicateGroup>(duplicates.Except(d2).ToList());

            var cts = containers
                .Where(c => c.Value.FirstFiles.Count > _modeSettings.MoreThanFileCount || c.Value.TheyThemselvesAreEqual)
                .Select(c => new DuplicateContainer(c.Key, c.Value.FirstFiles, c.Value.SecondFiles, c.Value.TheyThemselvesAreEqual));

            if (_modeSettings.ShowOnlyIfAllFilesInContainerEqual)
                cts = cts.Where(c => c.FirstEqualCount == c.FirstContainerFilesCount || c.SecondEqualCount == c.SecondContainerFilesCount);

            cts = cts.OrderByDescending(d => d.SizeOfEqualFiles);

            foreach (var item in cts)
            {
                // Sort by Name
                item.FirstEqualFiles.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
                item.SecondEqualFiles.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
                if (item.FirstInfo.ContainerFiles is not null)
                {
                    var fiEquals = item.FirstEqualFiles.Select(f => new SimpleFileInfo(f));
                    item.FirstDiffrentFiles = item.FirstInfo.ContainerFiles.Except(fiEquals).ToArray();
                }
                if (item.SecondInfo.ContainerFiles is not null)
                {
                    var fiEquals = item.SecondEqualFiles.Select(f => new SimpleFileInfo(f));
                    item.SecondDiffrentFiles = item.SecondInfo.ContainerFiles.Except(fiEquals).ToArray();
                }
            }

            return new ReadOnlyCollection<DuplicateContainer>(cts.ToList());

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

        class ContainerInfo()
        {
            public List<ExtendedFileInfo> FirstFiles = new List<ExtendedFileInfo>();

            public List<ExtendedFileInfo> SecondFiles = new List<ExtendedFileInfo>();
            public bool TheyThemselvesAreEqual { get; internal set; }
        }
    }
}
