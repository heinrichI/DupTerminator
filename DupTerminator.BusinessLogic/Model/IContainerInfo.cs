using System.Text.Json.Serialization;

namespace DupTerminator.BusinessLogic.Model
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ArchiveContainer), "archiveContainer")]
    [JsonDerivedType(typeof(PdfContainer), "pdfContainer")]
    [JsonDerivedType(typeof(DirectoryContainer), "directoryContainer")]
    public interface IContainerInfo
    {
        SimpleFileInfo[] Files { get; set; }
        int FilesCount { get; }
    }
}
