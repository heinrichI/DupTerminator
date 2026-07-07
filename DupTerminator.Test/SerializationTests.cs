using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;

namespace DupTerminator.Test
{
    public class SerializationTests
    {
        [Fact]
        public void Serialize_ChecksumDictionary_IncludesDiscriminator()
        {
            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            var afi = new ArchiveFileInfo
            {
                Name = "file1.txt",
                Path = @"C:\archive.zip\file1.txt",
                Size = 123,
                ArchiveFileName = "archive.zip",
                ArchivePath = @"C:\",
                ArchiveExtension = ".zip",
                ArchiveCRC = 0xDEADBEEF
            };
            dict["csum"] = new List<ExtendedFileInfo> { afi };

            var opts = BusinessLogic.SerializationHelpers.CreateOptions();
            string json = System.Text.Json.JsonSerializer.Serialize(dict, opts);

            Assert.Contains("\"$type\"", json);
            Assert.Contains("archive", json);
            Assert.Contains("ArchiveFileName", json);
            Assert.Contains("archive.zip", json);
        }

        [Fact]
        public void Deserialize_ChecksumDictionary_RestoresArchiveFileInfo()
        {
            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
            var afi = new ArchiveFileInfo
            {
                Name = "file1.txt",
                Path = @"C:\archive.zip\file1.txt",
                Size = 123,
                ArchiveFileName = "archive.zip",
                ArchivePath = @"C:\",
                ArchiveExtension = ".zip",
                ArchiveCRC = 0xDEADBEEF
            };
            dict["csum"] = new List<ExtendedFileInfo> { afi };

            var opts = BusinessLogic.SerializationHelpers.CreateOptions();
            string json = System.Text.Json.JsonSerializer.Serialize(dict, opts);
            var restored = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.IList<ExtendedFileInfo>>>(json, opts)
                ?? new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.IList<ExtendedFileInfo>>();

            Assert.True(restored.ContainsKey("csum"));
            var list = restored["csum"];
            Assert.Single(list);
            Assert.IsType<ArchiveFileInfo>(list[0]);
            var r = (ArchiveFileInfo)list[0];
            Assert.Equal("archive.zip", r.ArchiveFileName);
            Assert.Equal(@"C:\", r.ArchivePath);
        }
    }
}