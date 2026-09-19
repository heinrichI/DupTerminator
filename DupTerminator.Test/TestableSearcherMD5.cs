using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;

namespace DupTerminator.Test
{
    public class TestableSearcherMD5 : SearcherMD5
    {
        private readonly ConcurrentDictionary<string, IList<ExtendedFileInfo>> _fakeDictionary;

        public TestableSearcherMD5(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            IMd5Repository md5Repository,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            ILogger<SearcherMD5> logger,
            ConcurrentDictionary<string, IList<ExtendedFileInfo>> fakeDictionary)
            : base(locations, searchSetting, md5Repository, windowsUtil,
                   archiveService, pdfService, archiveInfoRepository, pdfInfoRepository, logger)
        {
            _fakeDictionary = fakeDictionary;
        }

        protected override Task<ConcurrentDictionary<string, IList<ExtendedFileInfo>>> GetChecksumDictionaryAsync(
            ReadOnlyCollection<SearchPath> locations,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            progress?.Report(new ProgressDto { Path = "test", State = "Done" });
            return Task.FromResult(_fakeDictionary);
        }
    }
}
