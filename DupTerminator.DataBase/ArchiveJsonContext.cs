using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    [JsonSerializable(typeof(IEnumerable<ArchiveFileInfo>))]
    [JsonSerializable(typeof(ArchiveFileInfo[]))]
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
