using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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


            //var t = Resolve(checksumDictionary);

            //var firtstLevelContainer = checksumDictionary.SelectMany(c => c.Value).Select(m => m.Container).Distinct();
            //var secondLevelContainer = firtstLevelContainer.Select(m => m.Container).Distinct();
            //var sorted = checksumDictionary
            //    .OrderBy(c => firtstLevelContainer.Contains(c.Value.First()))
            //    .ThenBy(c => secondLevelContainer.Contains(c.Value.First()));

            // 1. Сортируем группы дубликатов по глубине вложенности (сначала самые верхние)
            //var sorted = checksumDictionary
            //    .OrderBy(kvp => GetMaxDepth(kvp))
            //    .ToList();


            Dictionary<ContainerPairKey, ContainerInfo> containers = new Dictionary<ContainerPairKey, ContainerInfo>();
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

                            //если это сами контейнеры
                            ContainerPairKey key = new ContainerPairKey(first, second);
                            if (containers.ContainsKey(key))
                            {
                                containers[key].TheyThemselvesAreEqual = true;
                                containers[key].FirstFiles.Clear();
                                containers[key].SecondFiles.Clear();
                            }

                            ContainerPairKey containersKey = new ContainerPairKey(first.Container, second.Container, first, second);
                            // Initialize the list if the key doesn't exist
                            if (!containers.TryGetValue(containersKey, out ContainerInfo value))
                            {
                                value = new ContainerInfo();
                                containers[containersKey] = value;
                            }

                            if (value.TheyThemselvesAreEqual)
                                continue;

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

            HashSet<ContainerPairKey> forRemove = new ();
            foreach (var container in containers)
            {
                if (container.Key.First is not null && container.Key.Second is not null
                    && container.Key.First.Container is not null && container.Key.Second.Container is not null
                    && container.Key.First.Container is not DirectoryContainer && container.Key.Second.Container is not DirectoryContainer)
                {
                    ContainerPairKey containersKey = new ContainerPairKey(container.Key.First.Container, container.Key.Second.Container);
                    if (containers.ContainsKey(containersKey) && !forRemove.Contains(containersKey))
                    {
                        forRemove.Add(container.Key);
                    }
                }
            }
            foreach (var item in forRemove)
            {
                containers.Remove(item);
            }


            var cts = containers
                .Where(c => c.Value.FirstFiles.Count > _modeSettings.MoreThanFileCount || c.Value.TheyThemselvesAreEqual)
                .Select(c => new DuplicateContainer(c.Key, c.Value.FirstFiles, c.Value.SecondFiles, c.Value.TheyThemselvesAreEqual));

            if (_modeSettings.ShowOnlyIfAllFilesInContainerEqual)
                cts = cts.Where(c => c.FirstEqualCount == c.Key.FirstContainerFiles.Length || c.SecondEqualCount == c.Key.SecondContainerFiles.Length);

            cts = cts.OrderByDescending(d => d.SizeOfEqualFiles);

            foreach (DuplicateContainer item in cts)
            {
                // Sort by Name
                //item.FirstEqualFiles.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
                //item.SecondEqualFiles.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
                //item.FirstEqualFiles = item.FirstEqualFiles.OrderBy(f => f.Name).ToHashSet();
                //item.SecondEqualFiles = item.SecondEqualFiles.OrderBy(f => f.Name).ToHashSet();

                if (item.Key.FirstContainerFiles is not null)
                {
                    var fiEquals = item.FirstEqualFiles.Select(f => new SimpleFileInfo(f));
                    item.FirstDiffrentFiles = item.Key.FirstContainerFiles.Except(fiEquals).ToArray();
                }
                if (item.Key.SecondContainerFiles is not null)
                {
                    var fiEquals = item.SecondEqualFiles.Select(f => new SimpleFileInfo(f));
                    item.SecondDiffrentFiles = item.Key.SecondContainerFiles.Except(fiEquals).ToArray();
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
        

        private ExtendedFileInfo GetRootContainer(ExtendedFileInfo file)
        {
            var current = file;
            while (current.Container != null)
            {
                current = current.Container;
            }
            return current;
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
            public SortedSet<ExtendedFileInfo> FirstFiles = new SortedSet<ExtendedFileInfo>();

            public SortedSet<ExtendedFileInfo> SecondFiles = new SortedSet<ExtendedFileInfo>();
            public bool TheyThemselvesAreEqual { get; internal set; }
        }
    }
}
