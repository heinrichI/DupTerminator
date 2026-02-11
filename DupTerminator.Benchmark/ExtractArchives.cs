using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.Extensions.Logging.Abstractions;
using SevenZipExtractor;

namespace DupTerminator.Benchmark
{
    [NativeMemoryProfiler]
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 1, invocationCount: 10)]
    public class ExtractArchives
    {
        [Benchmark]
        public void ExtractArchives2()
        {
            ArchiveService archiveService = new ArchiveService(NullLogger<ArchiveService>.Instance);
            var streams = archiveService.GetStreams(new BusinessLogic.Model.ExtendedFileInfo
            {
                //Path = "i:\\Iron Man\\Invincible Iron Man\\Invincible Iron Man (001-033+500-527&Annuals)(2008-2012) GetComics.INFO.zip"
                Path = "i:\\Iron Man\\Invincible Iron Man\\Invincible Iron Man Vol. 3 001-011 (2017).zip"
            }, e => e.ToLower() == ".jpg", CancellationToken.None);
            foreach (var stream in streams)
            {
                stream.Item2.Dispose();
            }
            //var streams2 = archiveService.GetStreams(new BusinessLogic.Model.ExtendedFileInfo
            //{
            //    Path = "i:\\Iron Man\\Invincible Iron Man\\Invincible Iron Man Vol. 1.zip"
            //}, e => true, CancellationToken.None);
            //foreach (var stream in streams2)
            //{
            //    stream.Item2.Dispose();
            //}
        }
    }
}
