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
    public class SearcherPhashSearchContainer : SearcherPhashBase, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _locations;
        private readonly PHashSearchImageSettings _pHashSearchImageSettings;
        private readonly IMIHFactory _mIHFactory;
        private readonly Stopwatch _stopwatch = new();
        public SearcherPhashSearchContainer(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            PHashSearchImageSettings pHashSearchImageSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger<Searcher> logger) : base(searchSetting, new PHashSettings(), mIHFactory, pHashService, phashRepository, archiveService, pdfService, windowsUtil, logger)
        {
            _locations = locations;
            _pHashSearchImageSettings = pHashSearchImageSettings;
            _mIHFactory = mIHFactory;
        }

        public async Task<ReadOnlyCollection<DuplicateContainer>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            if (Directory.Exists(_pHashSearchImageSettings.Target))
            {
                DirectoryInfo di = new System.IO.DirectoryInfo(_pHashSearchImageSettings.Target);
                var dFiles = di.GetFiles();
                var files3 = dFiles.Select(f => new ExtendedFileInfo()
                {
                    Size = Convert.ToUInt64(f.Length),
                    Name = f.Name,
                    Path = f.FullName,
                    LastAccessTime = f.LastAccessTime,
                    LastWriteTime = f.LastWriteTime,
                    DirectoryName = f.DirectoryName,
                    Extension = f.Extension,
                    ContainerFilesCount = dFiles.Length
                });
                foreach (var item in files3)
                {
                }
            }
            else if (_archiveService.IsArchiveFile(_pHashSearchImageSettings.Target))
            {
            }

                var target = _pHashService.CalculatePHash(_pHashSearchImageSettings.Target);

                ConcurrentDictionary<ulong, IList<PHashFileInfo>> checksumDictionary = await CalculateChecksum(_locations, progress, cancelToken);

                if (checksumDictionary.Any())
                {
                    List<DuplicateContainer>? resultList = new List<DuplicateContainer>();
                    using (var mih = _mIHFactory.Create())
                    {
                        progress?.Report(new ProgressDto
                        {
                            State = "Start train MIH",
                            RemainSize = string.Empty,
                        });

                        mih.Update(checksumDictionary);
                        mih.Train(wordLength: _pHashSearchImageSettings.WordLength, threshold: _pHashSearchImageSettings.HammingDistance);

                        progress?.Report(new ProgressDto
                        {
                            State = "Ended train MIH",
                            RemainSize = string.Empty,
                        });


                        (ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)[]? resultQuery = mih.Query(target.phash).ToArray();

                        foreach ((ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance) queryItem in resultQuery)
                        {
                            foreach (var fileItem in queryItem.FileInfos)
                            {
                                var isi = new PHashFileInfoSearchItem(fileItem, queryItem.HammingDistance);
                                //resultList.Add(isi);
                            }
                        }
                    }
                    //resultList.Sort((x, y) => x.HammingDistance.CompareTo(y.HammingDistance));
                    return new ReadOnlyCollection<DuplicateContainer>(resultList);
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
