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

        public async Task<Collection<DuplicateContainer>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
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


            Dictionary<ContainerPairKey, LocalContainerInfo> containers = new Dictionary<ContainerPairKey, LocalContainerInfo>();
            
            _logger.LogInformation($"🔍 [MD5Container] START PROCESSING {checksumDictionary.Count} CHECKSUM GROUPS");
            
            // Pass 1: record content matches (files inside containers)
            foreach (KeyValuePair<string, IList<ExtendedFileInfo>> pair in checksumDictionary)
            {
                if (pair.Value.Count <= 1)
                    continue;

                IList<ExtendedFileInfo> files = pair.Value;
                for (int i = 0; i < files.Count; i++)
                {
                    for (int j = i + 1; j < files.Count; j++)
                    {
                        ExtendedFileInfo first = files[i];
                        ExtendedFileInfo second = files[j];
                        
                        // Debug.WriteLine($"📄 Pair found: #{pair.Key} FIRST={first.Path} SECOND={second.Path}");

                        // Whether this "file" is itself a container (the archive/zip itself)
                        bool firstIsContainer = first is ArchiveContainer || first is PdfContainer || first is DirectoryContainer || (first is DupTerminator.BusinessLogic.Model.ContainerInfo ci1 && ci1.Container != null);
                        bool secondIsContainer = second is ArchiveContainer || second is PdfContainer || second is DirectoryContainer || (second is DupTerminator.BusinessLogic.Model.ContainerInfo ci2 && ci2.Container != null);

                        // Skip files from the same container (by reference or by path)
                        // But if they're different containers, process them
                        if (firstIsContainer && secondIsContainer)
                        {
                            // Both are containers themselves - this is a container-to-container comparison
                            // Use the 4-arg constructor to track that both First and Second ARE the containers
                            ContainerPairKey key = new ContainerPairKey(first, second, first, second);
                            if (!containers.TryGetValue(key, out LocalContainerInfo info))
                            {
                                info = new LocalContainerInfo();
                                containers[key] = info;
                            }
                            // Don't skip - we want to track container pairs even if they're equal
                            if (info.FirstFiles.Count == 0 && info.SecondFiles.Count == 0)
                            {
                                info.FirstFiles.Add(first);
                                info.SecondFiles.Add(second);
                            }
                            continue;
                        }

                        // Skip files from the same container (by reference or by path) for non-container files
                        if (firstIsContainer || secondIsContainer)
                        {
                            // One is container, one is file inside - skip for now
                            continue;
                        }

                        // Regular files (not containers) - check same container filter
                        if (first.Container == second.Container)
                            continue;
                        if (first.Container != null && second.Container != null && first.Container.Path == second.Container.Path)
                            continue;

                        // Pass the CONTAINERS, not the files, so ContainerPairKey knows which containers we're comparing
                        ContainerPairKey containersKey = new ContainerPairKey(
                            first.Container ?? first, 
                            second.Container ?? second);
                        // Initialize the list if the key doesn't exist
                        if (!containers.TryGetValue(containersKey, out LocalContainerInfo value))
                        {
                            value = new LocalContainerInfo();
                            containers[containersKey] = value;
                        }

                        // Assuming you want to add all files from the current checksum group (pair.Value)
                        //value.AddRange([first, second]);
                        if (!value.FirstFiles.Contains(first))
                        {
                            if (containersKey.WasSwapped)
                            {
                                value.FirstFiles.Add(second);
                                value.SecondFiles.Add(first);
                            }
                            else
                            {
                                value.FirstFiles.Add(first);
                                value.SecondFiles.Add(second);
                            }
                        }
                    }
                }
            }

            // Pass 2: mark containers that are themselves identical (same checksum)
            foreach (KeyValuePair<string, IList<ExtendedFileInfo>> pair2 in checksumDictionary)
            {
                if (pair2.Value.Count <= 1)
                    continue;

                IList<ExtendedFileInfo> files2 = pair2.Value;
                for (int i2 = 0; i2 < files2.Count; i2++)
                {
                    for (int j2 = i2 + 1; j2 < files2.Count; j2++)
                    {
                        ExtendedFileInfo first = files2[i2];
                        ExtendedFileInfo second = files2[j2];

                        // DEBUG LOG FOR TARGET FILES
                        if (first.Path.Contains("Django - Zorro v01") || second.Path.Contains("Django - Zorro v01"))
                        {
                            _logger.LogInformation($"🔍 DEBUG TARGET FILE: {first.Path}");
                            _logger.LogInformation($"   Type: {first.GetType().Name}");
                            _logger.LogInformation($"   Is ArchiveContainer: {first is ArchiveContainer}");
                            _logger.LogInformation($"   Is ContainerInfo: {first is DupTerminator.BusinessLogic.Model.ContainerInfo}");
                            _logger.LogInformation($"   Has Files: {(first as DupTerminator.BusinessLogic.Model.ContainerInfo)?.Files?.Length ?? 0} files");
                            _logger.LogInformation($"   Checksum: {pair2.Key}");
                        }

                        // Only handle cases where the files ARE containers themselves
                        if (!(first is ArchiveContainer || first is PdfContainer || first is DirectoryContainer))
                        {
                            // Also handle generic ContainerInfo if it has Files
                            if (!(first is DupTerminator.BusinessLogic.Model.ContainerInfo ci) || ci.Files == null || ci.Files.Length == 0)
                                continue;
                        }

                        // When first and second ARE containers, use 4-arg constructor with them as both container and child
                        ContainerPairKey key = new ContainerPairKey(first, second, first, second);
                        if (!containers.TryGetValue(key, out LocalContainerInfo info))
                        {
                            info = new LocalContainerInfo();
                            containers[key] = info;
                        }
                        info.TheyThemselvesAreEqual = true;
                        info.FirstFiles.Clear();
                        info.SecondFiles.Clear();

                        _logger.LogDebug($"TheyThemselvesAreEqual: {first.Path} == {second.Path}");
                    }
                }
            }

            // PASS 2.5: Auto detect full container matches when all files are equal
            _logger.LogInformation($"🔍 [MD5Container] PASS 2.5: Auto detecting 100% identical containers");
            
            foreach (var containerEntry in containers.ToList())
            {
                if (containerEntry.Value.TheyThemselvesAreEqual)
                    continue;

                int totalFilesFirst = containerEntry.Key.FirstContainerFiles?.Length ?? 0;
                int totalFilesSecond = containerEntry.Key.SecondContainerFiles?.Length ?? 0;
                int equalFilesCount = containerEntry.Value.FirstFiles.Count;

                // If ALL files from both containers are matching - containers themselves are identical
                if (equalFilesCount == totalFilesFirst && equalFilesCount == totalFilesSecond && totalFilesFirst > 0)
                {
                    containerEntry.Value.TheyThemselvesAreEqual = true;
                    containerEntry.Value.FirstFiles.Clear();
                    containerEntry.Value.SecondFiles.Clear();
                    
                    _logger.LogInformation($"✅ Auto detected identical containers: {containerEntry.Key.First.Path} == {containerEntry.Key.Second.Path} ({equalFilesCount} files)");
                }
            }

            Debug.Assert(containers.All(c => c.Value.FirstFiles.Count == c.Value.SecondFiles.Count), "All entries must have equal FirstFiles and SecondFiles count");

            // PASS 3: Remove all child file matches when containers themselves are 100% identical
            _logger.LogInformation($"🔍 [MD5Container] PASS 3: Removing child entries for fully identical containers");
            
            var fullyIdenticalContainers = containers
                .Where(c => c.Value.TheyThemselvesAreEqual)
                .Select(c => c.Key)
                .ToList();

            int removedCount = 0;
            foreach (var identicalPair in fullyIdenticalContainers)
            {
                var keysToRemove = containers
                    .Where(kvp => 
                        !kvp.Value.TheyThemselvesAreEqual &&
                        (IsInsideContainer(kvp.Key.First, identicalPair.First) && IsInsideContainer(kvp.Key.Second, identicalPair.Second) ||
                         IsInsideContainer(kvp.Key.First, identicalPair.Second) && IsInsideContainer(kvp.Key.Second, identicalPair.First))
                    )
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    containers.Remove(key);
                    removedCount++;
                }
            }

            _logger.LogInformation($"✅ Removed {removedCount} child file entries for fully identical containers");


            //HashSet<ContainerPairKey> forRemove = new ();
            //foreach (var container in containers)
            //{
            //    if (container.Key.First is not null && container.Key.Second is not null
            //        && container.Key.First.Container is not null && container.Key.Second.Container is not null
            //        && container.Key.First.Container is not DirectoryContainer && container.Key.Second.Container is not DirectoryContainer)
            //    {
            //        ContainerPairKey containersKey = new ContainerPairKey(container.Key.First.Container, container.Key.Second.Container);
            //        if (containers.ContainsKey(containersKey) && !forRemove.Contains(containersKey))
            //        {
            //            forRemove.Add(container.Key);
            //        }
            //    }
            //}
            //foreach (var item in forRemove)
            //{
            //    containers.Remove(item);
            //}


            var cts = containers
                .Where(c => c.Value.FirstFiles.Count > _modeSettings.MoreThanFileCount || c.Value.TheyThemselvesAreEqual)
                .Select(c => new DuplicateContainer(c.Key, c.Value.FirstFiles, c.Value.SecondFiles, c.Value.TheyThemselvesAreEqual));

            if (_modeSettings.ShowOnlyIfAllFilesInContainerEqual)
                cts = cts.Where(c => c.FirstEqualCount == (c.Key.FirstContainerFiles?.Length ?? 0) || c.SecondEqualCount == (c.Key.SecondContainerFiles?.Length ?? 0));

            var ctsList = cts.OrderByDescending(d => d.SizeOfEqualFiles).ToList();

            foreach (DuplicateContainer item in ctsList)
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



            var filteredList = ctsList;

            // Удаляем пары, которые покрываются более крупными контейнерами
            for (int i = filteredList.Count - 1; i >= 0; i--)
            {
                var current = filteredList[i];
                bool isCovered = false;

                foreach (var other in filteredList)
                {
                    if (ReferenceEquals(current, other))
                        continue;

                    if (IsCoveredByContainerPair(current, other))
                    {
                        isCovered = true;
                        break;
                    }
                }

                if (isCovered)
                    filteredList.RemoveAt(i);
            }

            Debug.WriteLine($"✅ FINAL RESULTS: Found {filteredList.Count} duplicate container pairs");
            foreach (var item in filteredList)
            {
                Debug.WriteLine($"✅ DUPLICATE: [{item.Key.First.Path}] <==> [{item.Key.Second.Path}] (TheyThemselvesAreEqual: {item.TheyThemselvesAreEqual}, Files: {item.FirstEqualCount})");
            }

            return new Collection<DuplicateContainer>(filteredList);

            //return new ReadOnlyCollection<DuplicateContainer>(cts.ToList());

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

        bool IsInsideContainer(ExtendedFileInfo file, ExtendedFileInfo container)
        {
            return file.Equals(container) || file.GetAncestorContainers().Contains(container);
        }

        bool IsCoveredByContainerPair(DuplicateContainer child, DuplicateContainer parent)
        {
            // Проверяем, что child.First находится внутри parent.First (или совпадает)
            bool firstCovered = child.First.Equals(parent.First) ||
                                child.First.GetAncestorContainers().Contains(parent.First);

            // Аналогично для Second
            bool secondCovered = child.Second.Equals(parent.Second) ||
                                 child.Second.GetAncestorContainers().Contains(parent.Second);

            // Исключаем саму себя
            if (child.Key.Equals(parent.Key))
                return false;

            return firstCovered && secondCovered;
        }

        //private int GetMaxDepth(KeyValuePair<string, IList<ExtendedFileInfo>> kvp)
        //{
        //    if (kvp.Value == null || kvp.Value.Count == 0)
        //        return 0;

        //    int maxDepth = 0;

        //    foreach (var file in kvp.Value)
        //    {
        //        int currentDepth = 0;
        //        var currentContainer = file.Container;

        //        // Идем вглубь, пока Container не null
        //        while (currentContainer != null)
        //        {
        //            currentDepth++;
        //            currentContainer = currentContainer.Container;
        //        }

        //        if (currentDepth > maxDepth)
        //            maxDepth = currentDepth;
        //    }

        //    return maxDepth;
        //}

        //private ExtendedFileInfo GetRootContainer(ExtendedFileInfo file)
        //{
        //    var current = file;
        //    while (current.Container != null)
        //    {
        //        current = current.Container;
        //    }
        //    return current;
        //}

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

        class LocalContainerInfo
        {
            public SortedSet<ExtendedFileInfo> FirstFiles = new SortedSet<ExtendedFileInfo>();

            public SortedSet<ExtendedFileInfo> SecondFiles = new SortedSet<ExtendedFileInfo>();
            public bool TheyThemselvesAreEqual { get; internal set; }
        }
    }
}
