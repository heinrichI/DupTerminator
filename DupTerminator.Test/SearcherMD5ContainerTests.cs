using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.Test
{
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DupTerminator.BusinessLogic;
    using DupTerminator.BusinessLogic.Abstraction;
    using DupTerminator.BusinessLogic.Model;
    using DupTerminator.BusinessLogic.Model.Modes;
    using DupTerminator.DataBase;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class SearcherMD5ContainerTests
    {
        // -----------------------------------------------------------------------
        // Common mocks – they are the same for every test, only the checksum
        // dictionary changes.
        // -----------------------------------------------------------------------
        private readonly Mock<IMd5Repository> _md5Repo = new();
        private readonly Mock<IArchiveInfoRepository> _archiveInfoRepo = new();
        private readonly Mock<IPdfInfoRepository> _pdfInfoRepo = new();
        private readonly Mock<IWindowsUtil> _windowsUtil = new();
        private readonly Mock<IArchiveService> _archiveService = new();
        private readonly Mock<IPdfService> _pdfService = new();
        private readonly ILogger<SearcherMD5> _logger = NullLogger<SearcherMD5>.Instance;

        private readonly ReadOnlyCollection<SearchPath> _locations = new(new[] { TestFactory.DummySearchPath() });

        private readonly SearchSetting _searchSetting = TestFactory.DummySearchSetting();

        // -----------------------------------------------------------------------
        // 1️⃣  Empty checksum dictionary → empty result
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_ReturnsEmpty_WhenNoDuplicates()
        {
            // arrange
            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            var sut = new TestableSearcherMD5Container(
                _locations,
                _searchSetting,
                TestFactory.DefaultSettings(),
                _md5Repo.Object,
                _archiveInfoRepo.Object,
                _pdfInfoRepo.Object,
                _windowsUtil.Object,
                _archiveService.Object,
                _pdfService.Object,
                _logger,
                dict);

            var progress = new Progress<ProgressDto>();
            var token = CancellationToken.None;

            // act
            var result = await sut.StartAsync(progress, token);

            Assert.Equal(1, result.Count);
            Assert.Equal(46, result[0].FirstEqualCount);
            Assert.Equal(46, result[0].SecondEqualCount);
            Assert.Equal(18179955, result[0].SizeOfEqualFiles);
            Assert.Equal(46, result[0].FirstContainerFilesCount);
            Assert.Equal("F:\\E\\SourceC#My\\TestComicContainer\\Papyrus T22 - La Prisonniere de Sekhmet  [De Gieter] 46.pdf", result[0].First.Path);
            Assert.Equal(new DateTime(638786702386784573), result[0].First.LastWriteTime);
            Assert.Equal("Papyrus T22 - La Prisonniere de Sekhmet  [De Gieter] 46.pdf", result[0].First.Name);
            Assert.Equal(".pdf", result[0].First.Extension);
            Assert.Equal(18206587U, result[0].First.Size);
            Assert.Equal("F:\\E\\SourceC#My\\TestComicContainer\\Papyrus T22 46.cbz", result[0].Second.Path);
            Assert.Equal(new DateTime(638901236522416697), result[0].Second.LastWriteTime);
            Assert.Equal("Papyrus T22 46.cbz", result[0].Second.Name);
            Assert.Equal(".cbz", result[0].Second.Extension);
            Assert.Equal(17389904U, result[0].Second.Size);
            Assert.Null(result[0].FirstDiffrentFiles);
            Assert.Null(result[0].SecondDiffrentFiles);
        }

        // -----------------------------------------------------------------------
        // 6️⃣  Two identical archives (same checksum) → TheyThemselvesAreEqual = true,
        //     no partial-match content shown
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_IdenticalContainerContent_WhenAllFilesMatch_MarkedAsTheyThemselvesAreEqual()
        {
            // arrange
            // Two archives with DIFFERENT checksums (different headers/metadata) 
            // but ALL internal files are 100% identical. This is the bug case we fixed.
            var archiveA = TestFactory.CreateContainer(@"C:\A\archive1.cbr", "archive1.cbr", 3);
            var archiveB = TestFactory.CreateContainer(@"C:\B\archive2.cbr", "archive2.cbr", 3);

            // All 3 files are identical between containers
            var fileA1 = TestFactory.CreateFile(@"C:\A\archive1.cbr\f1.txt", "f1.txt", 100, "hash1", archiveA, 3);
            var fileB1 = TestFactory.CreateFile(@"C:\B\archive2.cbr\f1.txt", "f1.txt", 100, "hash1", archiveB, 3);
            var fileA2 = TestFactory.CreateFile(@"C:\A\archive1.cbr\f2.txt", "f2.txt", 200, "hash2", archiveA, 3);
            var fileB2 = TestFactory.CreateFile(@"C:\B\archive2.cbr\f2.txt", "f2.txt", 200, "hash2", archiveB, 3);
            var fileA3 = TestFactory.CreateFile(@"C:\A\archive1.cbr\f3.txt", "f3.txt", 300, "hash3", archiveA, 3);
            var fileB3 = TestFactory.CreateFile(@"C:\B\archive2.cbr\f3.txt", "f3.txt", 300, "hash3", archiveB, 3);

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            // Important: NO entry for archives themselves - they have different checksums!
            dict["hash1"] = new List<ExtendedFileInfo> { fileA1, fileB1 };
            dict["hash2"] = new List<ExtendedFileInfo> { fileA2, fileB2 };
            dict["hash3"] = new List<ExtendedFileInfo> { fileA3, fileB3 };

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 0,
                ShowOnlyIfAllFilesInContainerEqual = false
            };

            var sut = new TestableSearcherMD5Container(
                _locations,
                _searchSetting,
                settings,
                _md5Repo.Object,
                _archiveInfoRepo.Object,
                _pdfInfoRepo.Object,
                _windowsUtil.Object,
                _archiveService.Object,
                _pdfService.Object,
                _logger,
                dict);

            // act
            var result = await sut.StartAsync(new Progress<ProgressDto>(), CancellationToken.None);

            // assert: exactly one result, automatically marked as fully identical
            var dup = Assert.Single(result);
            Assert.True(dup.TheyThemselvesAreEqual,
                "Containers with 100% matching files must be automatically flagged as TheyThemselvesAreEqual even with different container checksums.");
            Assert.Empty(dup.FirstEqualFiles);
            Assert.Empty(dup.SecondEqualFiles);
        }

        [Fact]
        public async Task StartAsync_IdenticalArchives_MarkedAsTheyThemselvesAreEqual()
        {
            // arrange
            // Two archives with the same MD5 checksum.
            // Their internal files also have identical checksums.
            var archiveA = new ArchiveContainer(new ExtendedFileInfo
            {
                Path = @"C:\A\archive.zip",
                Name = "archive.zip",
                Size = 1000,
                LastWriteTime = DateTime.UtcNow,
                Extension = ".zip",
                DirectoryName = @"C:\A"
            })
            {
                Files = new[]
                {
                    new ArchiveSimpleFileInfo { Name = "file1.txt", Path = @"C:\A\archive.zip\file1.txt", Size = 100 },
                    new ArchiveSimpleFileInfo { Name = "file2.txt", Path = @"C:\A\archive.zip\file2.txt", Size = 200 },
                }
            };
            archiveA.Path = @"C:\A\archive.zip";
            archiveA.Name = "archive.zip";
            archiveA.Size = 1000;
            archiveA.Extension = ".zip";

            var archiveB = new ArchiveContainer(new ExtendedFileInfo
            {
                Path = @"C:\B\archive.zip",
                Name = "archive.zip",
                Size = 1000,
                LastWriteTime = DateTime.UtcNow,
                Extension = ".zip",
                DirectoryName = @"C:\B"
            })
            {
                Files = new[]
                {
                    new ArchiveSimpleFileInfo { Name = "file1.txt", Path = @"C:\B\archive.zip\file1.txt", Size = 100 },
                    new ArchiveSimpleFileInfo { Name = "file2.txt", Path = @"C:\B\archive.zip\file2.txt", Size = 200 },
                }
            };
            archiveB.Path = @"C:\B\archive.zip";
            archiveB.Name = "archive.zip";
            archiveB.Size = 1000;
            archiveB.Extension = ".zip";

            // Also add the inner files as content matches (simulating the normal flow
            // where inner files are hashed too).
            var fileA1 = new ArchiveFileInfo { Path = @"C:\A\archive.zip\file1.txt", Name = "file1.txt", Size = 100, Container = archiveA };
            var fileB1 = new ArchiveFileInfo { Path = @"C:\B\archive.zip\file1.txt", Name = "file1.txt", Size = 100, Container = archiveB };
            var fileA2 = new ArchiveFileInfo { Path = @"C:\A\archive.zip\file2.txt", Name = "file2.txt", Size = 200, Container = archiveA };
            var fileB2 = new ArchiveFileInfo { Path = @"C:\B\archive.zip\file2.txt", Name = "file2.txt", Size = 200, Container = archiveB };

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            // The archives themselves share a checksum
            dict["archive_checksum"] = new List<ExtendedFileInfo> { archiveA, archiveB };
            // Their contents also share checksums
            dict["file1_checksum"] = new List<ExtendedFileInfo> { fileA1, fileB1 };
            dict["file2_checksum"] = new List<ExtendedFileInfo> { fileA2, fileB2 };

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 0,
                ShowOnlyIfAllFilesInContainerEqual = false
            };

            var sut = new TestableSearcherMD5Container(
                _locations,
                _searchSetting,
                settings,
                _md5Repo.Object,
                _archiveInfoRepo.Object,
                _pdfInfoRepo.Object,
                _windowsUtil.Object,
                _archiveService.Object,
                _pdfService.Object,
                _logger,
                dict);

            // act
            var result = await sut.StartAsync(new Progress<ProgressDto>(), CancellationToken.None);

            // assert: exactly one result representing the pair of identical archives
            var dup = Assert.Single(result);
            Assert.True(dup.TheyThemselvesAreEqual,
                "Archives with the same checksum must be flagged as TheyThemselvesAreEqual, not shown as partial content match.");
            // When TheyThemselvesAreEqual, content lists must be empty
            Assert.Empty(dup.FirstEqualFiles);
            Assert.Empty(dup.SecondEqualFiles);
        }

        [Fact]
        public async Task ZorroTest()
        {
            // ── Arrange ────────────────────────────────────────────────────────
            var json = SerializationHelpers.ReadJsonFromZip(
                Path.Combine(AppContext.BaseDirectory, "TestData", "Zorro.zip"),
                "Zorro.json");
            var checksumDict = TestFactory.LoadFromJson(json);

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 1,
                ShowOnlyIfAllFilesInContainerEqual = false
            };

            var sut = new TestableSearcherMD5Container(
                _locations,
                _searchSetting,
                settings,
                _md5Repo.Object,
                _archiveInfoRepo.Object,
                _pdfInfoRepo.Object,
                _windowsUtil.Object,
                _archiveService.Object,
                _pdfService.Object,
                _logger,
                checksumDict);

            var progress = new Progress<ProgressDto>();
            var token = CancellationToken.None;

            // act
            var result = await sut.StartAsync(progress, token);

            Assert.Equal(1, result.Count);

            // Django-Zorro v01 — две копии CBR (внутри ZIP и отдельно)
            var r0 = result[0];
            Assert.True(r0.TheyThemselvesAreEqual);
            Assert.Equal(223, r0.Key.FirstContainerFiles.Length);
            Assert.Equal(223, r0.Key.SecondContainerFiles.Length);
            Assert.Contains("Django - Zorro v01", r0.First.Path);
            Assert.Contains("Django - Zorro v01", r0.Second.Path);
        }
    }
}
