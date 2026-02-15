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

            // assert
            Assert.Empty(result);
        }

        // -----------------------------------------------------------------------
        // 2️⃣  Two files with same checksum, different containers → one DuplicateContainer
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_ReturnsDuplicateContainer_WhenFilesInDifferentContainers()
        {
            // arrange
            var containerA = TestFactory.CreateFile(@"C:\A\containerA.zip", "containerA.zip", 0, "cA");
            var containerB = TestFactory.CreateFile(@"C:\B\containerB.zip", "containerB.zip", 0, "cB");

            var file1 = TestFactory.CreateFile(@"C:\A\containerA.zip\file.txt", "file.txt", 123, "checksum1", containerA, 2);
            var file2 = TestFactory.CreateFile(@"C:\B\containerB.zip\file.txt", "file.txt", 123, "checksum1", containerB, 2);

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            dict["checksum1"] = new List<ExtendedFileInfo> { file1, file2 };

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

            // assert
            var dup = Assert.Single(result);
            Assert.Equal(containerA.Path, dup.FirstInfo.Path);
            Assert.Equal(containerB.Path, dup.SecondInfo.Path);
            Assert.Equal(1, dup.FirstEqualCount);
            Assert.Equal(1, dup.SecondEqualCount);
            Assert.False(dup.TheyThemselvesAreEqual);
        }

        // -----------------------------------------------------------------------
        // 3️⃣  Same container → should be ignored
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_IgnoresFilesFromSameContainer()
        {
            // arrange
            var container = TestFactory.CreateFile(@"C:\C\containerC.zip", "containerC.zip", 0, "cC");

            var file1 = TestFactory.CreateFile(@"C:\C\containerC.zip\file1.txt", "file1.txt", 10, "csum", container, 2);
            var file2 = TestFactory.CreateFile(@"C:\C\containerC.zip\file2.txt", "file2.txt", 10, "csum", container, 2);

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            dict["csum"] = new List<ExtendedFileInfo> { file1, file2 };

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

            // assert
            Assert.Empty(result); // because the two files belong to the same container
        }

        // -----------------------------------------------------------------------
        // 4️⃣  MoreThanFileCount filter
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_FiltersByMoreThanFileCount()
        {
            // arrange
            var containerA = TestFactory.CreateFile(@"C:\A\cA.zip", "cA.zip", 0, "cA");
            var containerB = TestFactory.CreateFile(@"C:\B\cB.zip", "cB.zip", 0, "cB");

            // Pair with only ONE matching file (should be filtered out)
            var fileA1 = TestFactory.CreateFile(@"C:\A\cA.zip\f1.txt", "f1.txt", 5, "sum1", containerA, 1);
            var fileB1 = TestFactory.CreateFile(@"C:\B\cB.zip\f1.txt", "f1.txt", 5, "sum1", containerB, 1);

            // Pair with TWO matching files (passes the filter)
            var fileA2 = TestFactory.CreateFile(@"C:\A\cA.zip\f2.txt", "f2.txt", 5, "sum2", containerA, 2);
            var fileB2 = TestFactory.CreateFile(@"C:\B\cB.zip\f2.txt", "f2.txt", 5, "sum2", containerB, 2);
            var fileA3 = TestFactory.CreateFile(@"C:\A\cA.zip\f3.txt", "f3.txt", 5, "sum2", containerA, 2);
            var fileB3 = TestFactory.CreateFile(@"C:\B\cB.zip\f3.txt", "f3.txt", 5, "sum2", containerB, 2);

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            dict["sum1"] = new List<ExtendedFileInfo> { fileA1, fileB1 };
            dict["sum2"] = new List<ExtendedFileInfo> { fileA2, fileB2, fileA3, fileB3 };

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 2, // require at least 2 equal files
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

            var progress = new Progress<ProgressDto>();
            var token = CancellationToken.None;

            // act
            var result = await sut.StartAsync(progress, token);

            // assert
            var dup = Assert.Single(result);
            // The pair that survived must be the one built from checksum "sum2"
            //Assert.Contains("sum2", dup.FirstEqualFiles.First().CheckSum);
        }

        // -----------------------------------------------------------------------
        // 5️⃣  ShowOnlyIfAllFilesInContainerEqual = true
        // -----------------------------------------------------------------------
        [Fact]
        public async Task StartAsync_RespectsShowOnlyIfAllFilesInContainerEqual()
        {
            // arrange
            var containerA = TestFactory.CreateFile(@"C:\A\cA.zip", "cA.zip", 0, "cA");
            var containerB = TestFactory.CreateFile(@"C:\B\cB.zip", "cB.zip", 0, "cB");

            // Container A has 3 files, B has 3 files, but only 2 of them match.
            var a1 = TestFactory.CreateFile(@"C:\A\cA.zip\a1.txt", "a1.txt", 10, "csum", containerA, 3);
            var a2 = TestFactory.CreateFile(@"C:\A\cA.zip\a2.txt", "a2.txt", 10, "csum", containerA, 3);
            var a3 = TestFactory.CreateFile(@"C:\A\cA.zip\a3.txt", "a3.txt", 10, "uniqueA", containerA, 3);

            var b1 = TestFactory.CreateFile(@"C:\B\cB.zip\b1.txt", "b1.txt", 10, "csum", containerB, 3);
            var b2 = TestFactory.CreateFile(@"C:\B\cB.zip\b2.txt", "b2.txt", 10, "csum", containerB, 3);
            var b3 = TestFactory.CreateFile(@"C:\B\cB.zip\b3.txt", "b3.txt", 10, "uniqueB", containerB, 3);

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            dict["csum"] = new List<ExtendedFileInfo> { a1, a2, b1, b2 };
            dict["uniqueA"] = new List<ExtendedFileInfo> { a3 };
            dict["uniqueB"] = new List<ExtendedFileInfo> { b3 };

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 0,
                ShowOnlyIfAllFilesInContainerEqual = true
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

            var progress = new Progress<ProgressDto>();
            var token = CancellationToken.None;

            // act
            var result = await sut.StartAsync(progress, token);
        }

        //содержимое контейнеров равно, но они нет, содержимое не выдается
        //контенейры самы равны, содержимое не выдается
        [Fact]
        public async Task StartAsync_ContentContentEqual_ButTheyDont()
        {
            // ── Arrange ────────────────────────────────────────────────────────
            // The JSON string is the one you posted in the question.
            // For brevity we embed it as a resource file; in a real project you
            // would keep it under a folder like "TestData/largePayload.json".
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "ContainerThemselfEqual.json"));
            var checksumDict = TestFactory.LoadFromJson(json);

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 0,
                ShowOnlyIfAllFilesInContainerEqual = true
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

            // -------------------------------------------------------------------
            // 3️⃣️⃣  Assertions – we do not check *every* container (there are
            //      dozens) but we verify a few important invariants that prove the
            //      algorithm behaved correctly.
            // -------------------------------------------------------------------
            // 1️⃣  Every checksum that appears in *both* containers must produce a
            //    DuplicateContainer.
            var expectedPairs = checksumDict
                .Where(kvp => kvp.Value.Count > 1)               // at least two files
                .SelectMany(kvp => kvp.Value)                    // flatten
                .GroupBy(f => f.Container.Path)                  // group by container path
                .Where(g => g.Count() == 2)                      // exactly two containers (A & B)
                .Select(g => g.Key)
                .ToArray();

            // The result should contain at least one container for each such pair.
            foreach (var containerPath in expectedPairs)
            {
                Assert.Contains(result, dc => dc.FirstInfo.Path == containerPath || dc.SecondInfo.Path == containerPath);
            }

            // 2️⃣  All DuplicateContainers must have non‑empty equal‑file lists.
            foreach (var dup in result)
            {
                Assert.NotEmpty(dup.FirstEqualFiles);
                Assert.NotEmpty(dup.SecondEqualFiles);
                // Size must be the sum of distinct file sizes (or the min of the two sums)
                var sumFirst = dup.FirstEqualFiles.Distinct().Sum(f => (decimal)f.Size);
                var sumSecond = dup.SecondEqualFiles.Distinct().Sum(f => (decimal)f.Size);
                var expectedSize = dup.TheyThemselvesAreEqual ? dup.FirstInfo.FileInfo.Size : Math.Min(sumFirst, sumSecond);
                Assert.Equal(expectedSize, dup.SizeOfEqualFiles);
            }

            // 3️⃣  The total number of duplicate containers should equal the number
            //    of distinct checksum groups that have files from *different* containers.
            var distinctChecksumWithCrossContainer = checksumDict
                .Count(kvp => kvp.Value
                    .Select(f => f.Container.Path)
                    .Distinct()
                    .Count() > 1);

            Assert.Equal(distinctChecksumWithCrossContainer, result.Count);
        }

        [Fact]
        public async Task StartAsync_Pdf_Cbz()
        {
            // ── Arrange ────────────────────────────────────────────────────────
            // The JSON string is the one you posted in the question.
            // For brevity we embed it as a resource file; in a real project you
            // would keep it under a folder like "TestData/largePayload.json".
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "PdfAndCbz.json"));
            var checksumDict = TestFactory.LoadFromJson(json);

            var settings = new MD5ContainerSettings
            {
                MoreThanFileCount = 0,
                ShowOnlyIfAllFilesInContainerEqual = true
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
            Assert.Equal(46, result[0].FirstEqualCount);
            Assert.Equal(46, result[0].SecondEqualCount);
            Assert.Equal(18179955, result[0].SizeOfEqualFiles);
            Assert.Equal(46, result[0].FirstInfo.ContainerFilesCount);
            Assert.Equal("F:\\E\\SourceC#My\\TestComicContainer\\Papyrus T22 - La Prisonniere de Sekhmet  [De Gieter] 46.pdf", result[0].FirstInfo.Path);
            Assert.Equal(new DateTime(638786702386784573), result[0].FirstInfo.FileInfo.LastWriteTime);
            Assert.Equal("Papyrus T22 - La Prisonniere de Sekhmet  [De Gieter] 46.pdf", result[0].FirstInfo.FileInfo.Name);
            Assert.Equal(".pdf", result[0].FirstInfo.FileInfo.Extension);
            Assert.Equal(18206587U, result[0].FirstInfo.FileInfo.Size);
            Assert.Equal("F:\\E\\SourceC#My\\TestComicContainer\\Papyrus T22 46.cbz", result[0].SecondInfo.Path);
            Assert.Equal(new DateTime(638901236522416697), result[0].SecondInfo.FileInfo.LastWriteTime);
            Assert.Equal("Papyrus T22 46.cbz", result[0].SecondInfo.FileInfo.Name);
            Assert.Equal(".cbz", result[0].SecondInfo.FileInfo.Extension);
            Assert.Equal(17389904U, result[0].SecondInfo.FileInfo.Size);
            Assert.Null(result[0].FirstDiffrentFiles);
            Assert.Null(result[0].SecondDiffrentFiles);
        }
    }
}
