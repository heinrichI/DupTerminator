using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;

namespace DupTerminator.Test
{
    // ---------------------------------------------------------------------------
    // 2️⃣  Small factory helpers to create objects used in the tests
    // ---------------------------------------------------------------------------
    static class TestFactory
    {
        public static ExtendedFileInfo CreateFile(
            string path,
            string name,
            ulong size,
            string checksum,
            ExtendedFileInfo? container = null,
            int containerFilesCount = 0)
        {
            var file = new ExtendedFileInfo
            {
                Path = path,
                Name = name,
                Size = size,
                //CheckSum = checksum,
                LastWriteTime = DateTime.UtcNow,
                DirectoryName = System.IO.Path.GetDirectoryName(path),
                Extension = System.IO.Path.GetExtension(path),
                Container = container ?? new ExtendedFileInfo { Path = string.Empty },
                ContainerFilesCount = containerFilesCount
            };
            return file;
        }

        public static SearchPath DummySearchPath() => new SearchPath("C:\\dummy", true, true);
        public static ReadOnlyCollection<SearchPath> DummyLocations()
        => new ReadOnlyCollection<SearchPath>(new[] { new SearchPath(@"C:\dummy", true, true) });


        public static SearchSetting DummySearchSetting() => new SearchSetting();

        public static MD5ContainerSettings DefaultSettings() => new MD5ContainerSettings
        {
            MoreThanFileCount = 0,
            ShowOnlyIfAllFilesInContainerEqual = false
        };

        /// <summary>
        /// Reads the JSON you posted and builds a <see cref="ConcurrentDictionary{string, IList{ExtendedFileInfo}}"/>
        /// that can be fed directly to <c>SearcherMD5Container.StartAsync</c>.
        /// </summary>
        public static ConcurrentDictionary<string, IList<ExtendedFileInfo>> LoadFromJson(string json)
        {
            // The JSON structure is:  checksum => array of file objects.
            // We deserialize to a dictionary of JsonElement and then map each element
            // to an <see cref="ExtendedFileInfo"/> instance.
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();

            foreach (var kvp in raw)
            {
                var checksum = kvp.Key;
                var files = new List<ExtendedFileInfo>();

                foreach (var el in kvp.Value.EnumerateArray())
                {
                    // Helper to read a property safely (null → default)
                    static T Get<T>(JsonElement e, string name) =>
                        e.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null
                            ? p.Deserialize<T>()
                            : default!;

                    var file = new ExtendedFileInfo
                    {
                        LastWriteTime = Get<DateTime>(el, nameof(ExtendedFileInfo.LastWriteTime)),
                        DirectoryName = Get<string>(el, nameof(ExtendedFileInfo.DirectoryName)),
                        Extension = Get<string>(el, nameof(ExtendedFileInfo.Extension)),
                        ContainerFilesCount = Get<int>(el, nameof(ExtendedFileInfo.ContainerFilesCount)),
                        Size = Get<ulong>(el, nameof(ExtendedFileInfo.Size)),
                        Name = Get<string>(el, nameof(ExtendedFileInfo.Name)),
                        Path = Get<string>(el, nameof(ExtendedFileInfo.Path))
                    };

                    // The container object is nested – we need to deserialize it recursively.
                    if (el.TryGetProperty(nameof(ExtendedFileInfo.Container), out var containerEl) &&
                        containerEl.ValueKind == JsonValueKind.Object)
                    {
                        file.Container = new ExtendedFileInfo
                        {
                            LastWriteTime = Get<DateTime>(containerEl, nameof(ExtendedFileInfo.LastWriteTime)),
                            DirectoryName = Get<string>(containerEl, nameof(ExtendedFileInfo.DirectoryName)),
                            Extension = Get<string>(containerEl, nameof(ExtendedFileInfo.Extension)),
                            ContainerFilesCount = Get<int>(containerEl, nameof(ExtendedFileInfo.ContainerFilesCount)),
                            Size = Get<ulong>(containerEl, nameof(ExtendedFileInfo.Size)),
                            Name = Get<string>(containerEl, nameof(ExtendedFileInfo.Name)),
                            Path = Get<string>(containerEl, nameof(ExtendedFileInfo.Path))
                        };
                    }

                    files.Add(file);
                }

                dict[checksum] = files;
            }

            return dict;
        }
    }
}
