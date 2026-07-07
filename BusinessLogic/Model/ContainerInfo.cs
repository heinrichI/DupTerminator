using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ArchiveContainer), "archiveContainer")]
    [JsonDerivedType(typeof(PdfContainer), "pdfContainer")]
    [JsonDerivedType(typeof(DirectoryContainer), "directoryContainer")]
    public class ContainerInfo : ExtendedFileInfo
    {
        [JsonInclude]
        public SimpleFileInfo[] Files { get; set; }
        [JsonIgnore]
        public int FilesCount => Files?.Length ?? 0;
    }
}
