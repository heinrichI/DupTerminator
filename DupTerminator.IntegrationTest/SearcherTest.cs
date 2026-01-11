using Xunit;
using DupTerminator.BusinessLogic;
using System;
using System.Collections.ObjectModel;
using Moq;
using DupTerminator.WindowsSpecific;
using DupTerminator.BusinessLogic.Abstraction;

namespace DupTerminator.IntegrationTest
{
    public class SearcherTest
    {
        [Fact]
        public async void TestDir()
        {
            Mock<IMd5Repository> dbManager = new Mock<IMd5Repository>();
            using (SearcherMD5 searcher = new Searcher(
                new ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)>(
                new (string DirectoryPath, bool SearchInSubdirectory)[]
                {
                    ("c:\\SourceOpen\\dupterminator-svn\\TestDir", true)
                }),
                null,
                new SearchSetting(),
                dbManager.Object,
                new WindowsUtil(),
                null, null))
            {
                await searcher.Start();

                Assert.Equal(1, searcher.Duplicates.Count);
            }
        }
    }
}