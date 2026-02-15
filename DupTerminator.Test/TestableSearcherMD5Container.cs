using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.DataBase;
using Microsoft.Extensions.Logging;

namespace DupTerminator.Test
{
    // ---------------------------------------------------------------------------
    // 1️⃣  Test‑specific subclass that lets us inject a fake checksum dictionary
    // ---------------------------------------------------------------------------
    public class TestableSearcherMD5Container : SearcherMD5Container
    {
        private readonly ConcurrentDictionary<string, IList<ExtendedFileInfo>> _fakeDictionary;
        private readonly int _delayMs; // optional delay to simulate long work

        public TestableSearcherMD5Container(
            ReadOnlyCollection<SearchPath> locations,
            SearchSetting searchSetting,
            MD5ContainerSettings modeSettings,
            IMd5Repository md5Repository,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            ILogger<SearcherMD5> logger,
            ConcurrentDictionary<string, IList<ExtendedFileInfo>> fakeDictionary,
            int delayMs = 0)
            : base(locations, searchSetting, modeSettings,
                   md5Repository, archiveInfoRepository, pdfInfoRepository,
                   windowsUtil, archiveService, pdfService, logger)
        {
            _fakeDictionary = fakeDictionary;
            _delayMs = delayMs;
        }

        // The original implementation is *protected* in the base class.
        protected override async Task<ConcurrentDictionary<string, IList<ExtendedFileInfo>>> GetChecksumDictionaryAsync(
            ReadOnlyCollection<SearchPath> locations,
            IProgress<ProgressDto> progress,
            CancellationToken cancelToken)
        {
            // Simulate a little work so that a cancelled token can be observed.
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancelToken);
            }

            // In a real implementation progress would be reported – we do a single report.
            progress?.Report(new ProgressDto { Path = "test", State = "Done" });

            return _fakeDictionary;
        }
    }
}
