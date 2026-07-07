using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic
{
    [JsonSerializable(typeof(ConcurrentDictionary<string, IList<ExtendedFileInfo>>))]
    [JsonSerializable(typeof(IList<ExtendedFileInfo>))]
    [JsonSerializable(typeof(IEnumerable<ArchiveFileInfo>))]
    [JsonSerializable(typeof(ArchiveFileInfo[]))]
    [JsonSerializable(typeof(ExtendedFileInfo))]
    [JsonSerializable(typeof(SimpleFileInfo[]))]
    [JsonSerializable(typeof(SimpleFileInfo))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ArchiveJsonContext : JsonSerializerContext
    {
        // Empty (source generator fills it)
    }
}
