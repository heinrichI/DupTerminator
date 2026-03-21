using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;


namespace DupTerminator.BusinessLogic
{
    public class SearcherPhashSearchImage : SearcherPhashBase, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly PHashSearchImageSettings _pHashSearchImageSettings;
        private readonly IMIHFactory _mIHFactory;
        private readonly Stopwatch _stopwatch = new();
        public SearcherPhashSearchImage(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            PHashSearchImageSettings pHashSearchImageSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<SearcherMD5> logger) : base(searchSetting, new PHashSettings(), mIHFactory, pHashService, phashRepository, archiveService, pdfService, windowsUtil, logger)
        {
            _locations = locations;
            _pHashSearchImageSettings = pHashSearchImageSettings;
            _mIHFactory = mIHFactory;
        }

        public async Task<ReadOnlyCollection<PHashFileInfoSearchItem>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            if (!File.Exists(_pHashSearchImageSettings.Target))
                return null;

            var target = _pHashService.CalculatePHash(_pHashSearchImageSettings.Target);

            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = await CalculateChecksum(_locations, progress, cancelToken);

            if (checksumDictionary.Any())
            {
                var resultList = new List<PHashFileInfoSearchItem>();
                using (var mih = _mIHFactory.Create())
                {
                    progress?.Report(new ProgressDto
                    {
                        State = $"Start train MIH for {checksumDictionary.Count} hashes",
                        RemainSize = string.Empty,
                    });

                    mih.Update(checksumDictionary);
                    mih.Train(wordLength: _pHashSearchImageSettings.WordLength, threshold: _pHashSearchImageSettings.HammingDistance);

                    progress?.Report(new ProgressDto
                    {
                        State = "Ended train MIH",
                        RemainSize = string.Empty,
                    });


                    (ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(target.phash.Value).ToArray();
                     
                    foreach ((ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance) queryItem in resultQuery)
                    {
                        foreach (var fileItem in queryItem.FileInfos)
                        {
                            if (fileItem.FileInfo.Path != _pHashSearchImageSettings.Target
                                && !resultList.Any(r => r.FileItem.FileInfo.Path == fileItem.FileInfo.Path))
                            {
                                var isi = new PHashFileInfoSearchItem(fileItem, queryItem.HammingDistance);
                                resultList.Add(isi);
                            }
                        }
                    }
                }
                resultList.Sort((x, y) => x.HammingDistance.CompareTo(y.HammingDistance));
                return new ReadOnlyCollection<PHashFileInfoSearchItem>(resultList);
            }

            return null;
        }
        public async Task<ReadOnlyCollection<PHashFileInfoSearchItem>> ReSearchAsync(ulong hash, IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = await CalculateChecksum(_locations, progress, cancelToken);

            if (checksumDictionary.Any())
            {
                var resultList = new List<PHashFileInfoSearchItem>();
                using (var mih = _mIHFactory.Create())
                {
                    progress?.Report(new ProgressDto
                    {
                        State = $"Start train MIH for {checksumDictionary.Count} hashes",
                        RemainSize = string.Empty,
                    });

                    mih.Update(checksumDictionary);
                    mih.Train(wordLength: _pHashSearchImageSettings.WordLength, threshold: _pHashSearchImageSettings.HammingDistance);

                    progress?.Report(new ProgressDto
                    {
                        State = "Ended train MIH",
                        RemainSize = string.Empty,
                    });


                    (ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(hash).ToArray();

                    foreach ((ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance) queryItem in resultQuery)
                    {
                        foreach (var fileItem in queryItem.FileInfos)
                        {
                            if (fileItem.FileInfo.Path != _pHashSearchImageSettings.Target
                                && !resultList.Any(r => r.FileItem.FileInfo.Path == fileItem.FileInfo.Path))
                            {
                                var isi = new PHashFileInfoSearchItem(fileItem, queryItem.HammingDistance);
                                resultList.Add(isi);
                            }
                        }
                    }
                }
                resultList.Sort((x, y) => x.HammingDistance.CompareTo(y.HammingDistance));
                return new ReadOnlyCollection<PHashFileInfoSearchItem>(resultList);
            }

            return null;
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
    }
}
