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
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DupTerminator.BusinessLogic
{
    public class Searcher : SearcherMD5Base, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;

        //private readonly DbArchiveService _dbArchiveService;
        private readonly ILogger<Searcher> _logger;
        private readonly ContainerComparer _containerComparer;

        //public ReadOnlyCollection<DuplicateGroup> Duplicates { get; private set; }



        //private CancellationTokenSource _cts;

        //IProgress<Tuple<int, string>> _progressSearchFile = new Progress<Tuple<int, string>>();

        //IProgress<Tuple<int, string>> _progressCalculateDuplicate = new Progress<Tuple<int, string>>();

        public Searcher(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            IMd5Repository md5Repository,
            IWindowsUtil windowsUtil,
            //IProgress<ProgressDto> progress,
            //CancellationToken cancellationToken,
            IArchiveService archiveService,
            IPdfService pdfService,
            //Service.DbArchiveService dbArchiveService,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            ILogger<Searcher> logger) : base(searchSetting, windowsUtil, md5Repository, archiveService, pdfService, archiveInfoRepository, pdfInfoRepository, logger)
        {
            _locations = locations;
            //_progress = progress;
            //_dbArchiveService = dbArchiveService;
            _logger = logger;
            _containerComparer = new ContainerComparer();
        }

        public class ContainerComparer : IEqualityComparer<ExtendedFileInfo>
        {
            public bool Equals(ExtendedFileInfo x, ExtendedFileInfo y)
            {
                if (x == null || y == null)
                    return false;
                return x.Extension == y.Extension &&
                       x.Size == y.Size &&
                       x.Name == y.Name &&
                       x.Path == y.Path;
            }

            public int GetHashCode(ExtendedFileInfo obj)
            {
                return HashCode.Combine(obj.Extension, obj.Size, obj.Name, obj.Path);
            }
        }


        public async Task<ReadOnlyCollection<DuplicateGroup>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            ConcurrentDictionary<string, IList<ExtendedFileInfo>> checksumDictionary = await GetChecksumDictionaryAsync(_locations, progress, cancelToken);

            IEnumerable<DuplicateGroup>? duplicates = checksumDictionary
                 .Where(pair => pair.Value.Count > 1)
                 .Select(pair => new DuplicateGroup(pair.Key, pair.Value));
            //.OrderByDescending(d => d.Files.Any(f => f.Container is null));

            var flat = duplicates.SelectMany(dg => dg.Files).ToArray();
            var containers = flat.Select(f => f.Container).Distinct();
            var intersect = containers.Intersect(flat, _containerComparer).ToList();
            var forRemove = duplicates.Where(d => d.Files.Any(f => intersect.Contains(f.Container)));
            var filtered = duplicates.Except(forRemove).ToList();

            //var withoutContainer = duplicates.SelectMany(f => f.Files).Where(d => d.Container is null);
            //var d2 = duplicates.Where(d => d.Files.Any(f => withoutContainer.Any(c => f.Container is not null && c.Path == f.Container.Path)));
            //if (d2 != null && d2.Any())
            //{
            //    _logger.LogInformation($"Контейнеров с дублями: {d2.Count()}");
            //}

            return new ReadOnlyCollection<DuplicateGroup>(filtered);
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
