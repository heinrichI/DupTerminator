using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;
using DupTerminator.DataBase.Extensions;
using DupTerminator.ImageHash.Extensions;
using DupTerminator.Pdf.Extensions;
using DupTerminator.WindowsSpecific;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SevenZipExtractor.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace DupTerminator.IntegrationTest
{
    public class ConsoleMessageService : IMessageService
    {
        public void DeletedOutdatedRecord(uint deleted) { }
    }

    public class SearcherTest(ITestOutputHelper _output)
    {
        [Fact]
        public async Task Zorro()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            var file1 = Path.Combine(dataDir, "Django - Zorro 001-007+TPB Vol. 1 (2014-2015) GetComics.INFO.zip");
            var file2 = Path.Combine(dataDir, "Django - Zorro v01 (2015) GetComics.INFO.cbr");
            if (!File.Exists(file1)) { _output.WriteLine($"SKIP: {file1} not found"); return; }
            if (!File.Exists(file2)) { _output.WriteLine($"SKIP: {file2} not found"); return; }

            // холодный запуск: Md5Repository кэширует MD5 в md5.db рядом с выходной директорией
            var md5Db = Path.Combine(AppContext.BaseDirectory, "md5.db");
            if (File.Exists(md5Db))
                File.Delete(md5Db);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IMessageService, ConsoleMessageService>();
            services.AddDataBase();
            services.AddPHashService();
            services.AddArchive();
            services.AddPdf();
            services.AddWindowsUtil();
            using var sp = services.BuildServiceProvider();
            var searcherLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<SearcherMD5>();

            var include = new ReadOnlyCollection<SearchPath>(new SearchPath[]
            {
                new SearchPath(dataDir, true, true)
            });

            using var searcher = new SearcherMD5(
                include,
                new SearchSetting(),
                sp.GetRequiredService<IMd5Repository>(),
                sp.GetRequiredService<IWindowsUtil>(),
                sp.GetRequiredService<IArchiveService>(),
                sp.GetRequiredService<IPdfService>(),
                sp.GetRequiredService<IArchiveInfoRepository>(),
                sp.GetRequiredService<IPdfInfoRepository>(),
                searcherLogger);

            var result = await searcher.StartAsync(new Progress<ProgressDto>(), CancellationToken.None);
            Assert.NotNull(result);

            _output.WriteLine($"Groups: {result.Count}");
            foreach (var group in result)
            {
                _output.WriteLine($"Group {group.Checksum}:");
                foreach (var f in group.Files)
                    _output.WriteLine($"  {f.Size} {f.Path}");
            }

            // дублей ровно два: общая jpg внутри zip и cbr v01 (внешний файл + копия в zip)
            Assert.Equal(2, result.Count);

            // группа 1: zSoU-Nerd.jpg — один и тот же файл внутри Zorro 003 и Zorro 004 (в zip)
            var jpgGroup = result.Single(g => g.Checksum == "d5490c5e06fe167045c26c0b7b4b8b49");
            Assert.Equal(2, jpgGroup.Files.Count());
            Assert.All(jpgGroup.Files, f =>
            {
                Assert.Equal("zSoU-Nerd.jpg", f.Name);
                Assert.Equal(1204289UL, f.Size);
                Assert.StartsWith(file1, f.Path, StringComparison.OrdinalIgnoreCase);
            });

            // группа 2: cbr v01 — внешний cbr побайтово совпадает с копией внутри zip
            var zorroV01Group = result.Single(g => g.Checksum == "314e76ba2adfb58c8b4fba11c06b27ed");
            Assert.Equal(2, zorroV01Group.Files.Count());
            var externalCbr = zorroV01Group.Files.Single(f => f.Path == file2);
            var cbrInZip = zorroV01Group.Files.Single(f => f.Path.StartsWith(file1, StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Django - Zorro v01 (2015) (digital) (The Magicians-Empire).cbr", cbrInZip.Name);
            Assert.Equal(786364019UL, externalCbr.Size);
            Assert.Equal(externalCbr.Size, cbrInZip.Size);
        }

        [Fact]
        public async Task TestDir()
        {
            var testDir = @"c:\SourceOpen\dupterminator-svn\TestDir";
            if (!Directory.Exists(testDir)) { _output.WriteLine($"SKIP: {testDir} not found"); return; }

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IMessageService, ConsoleMessageService>();
            services.AddDataBase();
            services.AddArchive();
            services.AddPdf();
            services.AddWindowsUtil();
            using var sp = services.BuildServiceProvider();
            var dirSearcherLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<SearcherMD5>();

            var include = new ReadOnlyCollection<SearchPath>(new SearchPath[]
            {
                new SearchPath(testDir, true, true)
            });

            using var searcher = new SearcherMD5(
                include,
                new SearchSetting(),
                sp.GetRequiredService<IMd5Repository>(),
                sp.GetRequiredService<IWindowsUtil>(),
                sp.GetRequiredService<IArchiveService>(),
                sp.GetRequiredService<IPdfService>(),
                sp.GetRequiredService<IArchiveInfoRepository>(),
                sp.GetRequiredService<IPdfInfoRepository>(),
                dirSearcherLogger);

            var result = await searcher.StartAsync(new Progress<ProgressDto>(), CancellationToken.None);
            _output.WriteLine($"Groups: {result?.Count ?? 0}");
            if (result is not null)
            {
                foreach (var group in result)
                {
                    _output.WriteLine($"Group {group.Checksum}:");
                    foreach (var f in group.Files)
                        _output.WriteLine($"  {f.Size} {f.Path}");
                }
            }
            Assert.Equal(1, result?.Count ?? 0);
        }
    }
}