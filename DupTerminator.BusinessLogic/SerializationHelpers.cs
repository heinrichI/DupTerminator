using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic
{
    public static class SerializationHelpers
    {
        public static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = false,
                TypeInfoResolver = ArchiveJsonContext.Default
            };
        }

        public static string SerializeChecksumDictionary(ConcurrentDictionary<string, IList<ExtendedFileInfo>> dict)
        {
            var opts = CreateOptions();
            return JsonSerializer.Serialize(dict, opts);
        }

        public static void SerializeChecksumDictionaryToFile(ConcurrentDictionary<string, IList<ExtendedFileInfo>> dict, string path)
        {
            var json = SerializeChecksumDictionary(dict);
            File.WriteAllText(path, json);
        }

        public static ConcurrentDictionary<string, IList<ExtendedFileInfo>> DeserializeChecksumDictionary(string json)
        {
            var opts = CreateOptions();
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, IList<ExtendedFileInfo>>>(json, opts)
                ?? new ConcurrentDictionary<string, IList<ExtendedFileInfo>>();
        }

        public static string ReadJsonFromZip(string zipPath, string entryName)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.GetEntry(entryName)
                ?? throw new FileNotFoundException($"Entry '{entryName}' not found in {zipPath}");
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}