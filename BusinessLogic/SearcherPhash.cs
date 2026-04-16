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
    public class SearcherPhash : SearcherPhashBase, IDisposable
    {
        private readonly ReadOnlyCollection<SearchPath> _includeLocations;
        private readonly ReadOnlyCollection<SearchPath> _excludeLocations;
        private readonly Stopwatch _stopwatch = new();


        // Use the shared singleton instance of MemoryPool<int>
        //MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;

        public SearcherPhash(
            ReadOnlyCollection<SearchPath> includeLocations,
            ReadOnlyCollection<SearchPath> excludeLocations,
            SearchSetting searchSetting,
            PHashSettings pHashSettings,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mIHFactory,
            ILogger logger) : base(searchSetting, pHashSettings, mIHFactory, pHashService, phashRepository, archiveService, pdfService, windowsUtil, logger)
        {
            _includeLocations = includeLocations;
            _excludeLocations = excludeLocations;
        }

        public async Task<ReadOnlyCollection<PHashDuplicateGroup>> StartAsync(IProgress<ProgressDto> progress, CancellationToken cancelToken)
        {
            _stopwatch.Restart();
       

            //if (GCSettings.LargeObjectHeapCompactionMode != GCLargeObjectHeapCompactionMode.CompactOnce)   // no-op if already set
            //{
            //    GCSettings.LargeObjectHeapCompactionMode =
            //        GCLargeObjectHeapCompactionMode.CompactOnce;
            //}

            List<PHashDuplicateGroup> duplicateGroups = await GetDuplicateGroupAsync(_includeLocations, _excludeLocations, progress, cancelToken);

            _stopwatch.Stop();
            _logger.LogInformation($"ElapsedTime: {_stopwatch.Elapsed}");

            if (duplicateGroups is null)
                return null;

            //var list = duplicateGroups.Select(d => new DuplicateGroup(Guid.NewGuid().ToString(), d.ToList())).ToList();
            var duplicateGroupsFiltered = duplicateGroups.Where(d => d.Count > 1).ToList();
            return new ReadOnlyCollection<PHashDuplicateGroup>(duplicateGroupsFiltered);
            //}

            //return new ReadOnlyCollection<PHashDuplicateGroup>(new PHashDuplicateGroup[0]); 
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
