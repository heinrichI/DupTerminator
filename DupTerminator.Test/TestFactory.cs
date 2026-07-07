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
using SerializationHelpers = DupTerminator.BusinessLogic.SerializationHelpers;

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
            ContainerInfo? container = null,
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
                Container = container ?? new ContainerInfo { Path = string.Empty, Name = string.Empty, Files = Array.Empty<SimpleFileInfo>() },
            };
            return file;
        }

        public static ContainerInfo CreateContainer(
            string path,
            string name,
            ulong size = 0)
        {
            return new ContainerInfo
            {
                Path = path,
                Name = name,
                Size = size,
                LastWriteTime = DateTime.UtcNow,
                DirectoryName = System.IO.Path.GetDirectoryName(path),
                Extension = System.IO.Path.GetExtension(path),
                Files = Array.Empty<SimpleFileInfo>()
            };
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
        /// Reads JSON with <c>$type</c> discriminators and builds a properly-typed
        /// dictionary, reconstructing <see cref="ContainerInfo.Files"/> from the file entries.
        /// <para/>
        /// For old JSON without discriminators, falls back to manual element parsing
        /// (creating correct C# types) and then rebuilds Files arrays.
        /// </summary>
        public static ConcurrentDictionary<string, IList<ExtendedFileInfo>> LoadFromJson(string json)
        {
            // Try polymorphic deserialization first (for JSON with $type discriminators)
            bool hasDiscriminators = json.Contains("$type", StringComparison.OrdinalIgnoreCase);
            if (hasDiscriminators)
            {
                using var doc = JsonDocument.Parse(json);
                return ReconstructFromPolymorphic(json);
            }

            // ── Fallback: manual JsonElement parsing for old JSON ──
            return ReconstructFromElements(json);
        }

        /// <summary>Deserializes JSON with $type discriminators via the real serializer.</summary>
        private static ConcurrentDictionary<string, IList<ExtendedFileInfo>> ReconstructFromPolymorphic(string json)
        {
            var dict = SerializationHelpers.DeserializeChecksumDictionary(json);
            // BUG FIX: RebuildAllContainerFiles перезаписывала Container.Files после корректной
            // десериализации. В словаре чексумм есть только файлы-дубликаты, поэтому пересобранные
            // Files-массивы были неполными (1 файл вместо реального количества). Это приводило к
            // ложным TheyThemselvesAreEqual=True в Pass 2.5 (.equalFilesCount == totalFilesFirst ==
            // totalFilesSecond при totalFiles=1). Десериализатор уже корректно восстанавливает
            // Container.Files из JSON, повторная сборка не нужна.
            return dict;
        }

        /// <summary>Manual parsing for old-format JSON (no $type discriminators).</summary>
        private static ConcurrentDictionary<string, IList<ExtendedFileInfo>> ReconstructFromElements(string json)
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var dict = new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();

            static T Prop<T>(JsonElement e, string name) =>
                e.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null
                    ? p.Deserialize<T>()! : default!;

            foreach (var kvp in raw)
            {
                var files = new List<ExtendedFileInfo>();

                foreach (var el in kvp.Value.EnumerateArray())
                {
                    var file = new ExtendedFileInfo
                    {
                        LastWriteTime = Prop<DateTime>(el, nameof(ExtendedFileInfo.LastWriteTime)),
                        DirectoryName = Prop<string>(el, nameof(ExtendedFileInfo.DirectoryName)),
                        Extension = Prop<string>(el, nameof(ExtendedFileInfo.Extension)),
                        Size = Prop<ulong>(el, nameof(ExtendedFileInfo.Size)),
                        Name = Prop<string>(el, nameof(ExtendedFileInfo.Name)),
                        Path = Prop<string>(el, nameof(ExtendedFileInfo.Path))
                    };

                    // Deserialize nested container
                    if (el.TryGetProperty(nameof(ExtendedFileInfo.Container), out var c) &&
                        c.ValueKind == JsonValueKind.Object)
                    {
                        file.Container = new ContainerInfo
                        {
                            LastWriteTime = Prop<DateTime>(c, nameof(ExtendedFileInfo.LastWriteTime)),
                            DirectoryName = Prop<string>(c, nameof(ExtendedFileInfo.DirectoryName)),
                            Extension = Prop<string>(c, nameof(ExtendedFileInfo.Extension)),
                            Size = Prop<ulong>(c, nameof(ExtendedFileInfo.Size)),
                            Name = Prop<string>(c, nameof(ExtendedFileInfo.Name)),
                            Path = Prop<string>(c, nameof(ExtendedFileInfo.Path))
                        };
                    }

                    files.Add(file);
                }

                dict[kvp.Key] = files;
            }

            // Rebuild Files arrays
            RebuildAllContainerFiles(dict);
            return dict;
        }

        /// <summary>
        /// Groups all <see cref="ExtendedFileInfo"/> entries by <c>Container.Path</c>
        /// and assigns the grouped <see cref="SimpleFileInfo"/> arrays to each container.
        /// </summary>
        private static void RebuildAllContainerFiles(ConcurrentDictionary<string, IList<ExtendedFileInfo>> dict)
        {
            var map = new Dictionary<string, List<SimpleFileInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in dict)
                foreach (var file in kvp.Value)
                {
                    var c = file.Container;
                    if (c == null || string.IsNullOrEmpty(c.Path)) continue;

                    if (!map.TryGetValue(c.Path, out var list))
                        map[c.Path] = list = new List<SimpleFileInfo>();
                    list.Add(new SimpleFileInfo(file));
                }

            foreach (var kvp in dict)
                foreach (var file in kvp.Value)
                {
                    var c = file.Container;
                    if (c == null) continue;
                    c.Files = map.TryGetValue(c.Path, out var list) ? list.ToArray() : Array.Empty<SimpleFileInfo>();
                }
        }
    }
}
