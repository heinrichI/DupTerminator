using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Xunit;

namespace DupTerminator.Test
{
    public class SearcherMD5Tests
    {
        private readonly Mock<IMd5Repository> _md5Repo = new();
        private readonly Mock<IArchiveInfoRepository> _archiveInfoRepo = new();
        private readonly Mock<IPdfInfoRepository> _pdfInfoRepo = new();
        private readonly Mock<IWindowsUtil> _windowsUtil = new();
        private readonly Mock<IArchiveService> _archiveService = new();
        private readonly Mock<IPdfService> _pdfService = new();
        private readonly ILogger<SearcherMD5> _logger = NullLogger<SearcherMD5>.Instance;

        private readonly ReadOnlyCollection<SearchPath> _locations = new(new[] { TestFactory.DummySearchPath() });
        private readonly SearchSetting _searchSetting = TestFactory.DummySearchSetting();

        [Fact]
        public async Task ContainerSameFileTest()
        {
            // ── Arrange ────────────────────────────────────────────────────────
            var json = SerializationHelpers.ReadJsonFromZip(
                Path.Combine(AppContext.BaseDirectory, "TestData", "ContainerSameFile.zip"),
                "ContainerSameFile.json");
            var checksumDict = TestFactory.LoadFromJson(json);

            var sut = new TestableSearcherMD5(
                _locations,
                _searchSetting,
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

            // assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }
    }
}
