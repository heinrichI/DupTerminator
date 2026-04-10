using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Abstraction
{
    public interface IArchiveService
    {
        (ArchiveFileInfo, T)[] CalculateHashesInArchive<T>(ArchiveFileInfo[] data, Func<Stream, T> calculator);
        T CalculateHashInArchive<T>(ArchiveFileInfo fileInfo, Func<Stream, T> calculator);

        //IEnumerable<ExtendedFileInfo> GetHashesFromArchive(ExtendedFileInfo fileInfo);
        ArchiveFileInfo[] GetInfoFromArchive(ExtendedFileInfo archive, CancellationToken token, ulong? skipLessThan = null, bool archiveInArchive = false);

        IEnumerable<ArchiveFileInfo> GetInfoFromArchive(Stream stream, ArchiveFileInfo container, bool archiveInArchive = false);
        Stream GetStream(ArchiveFileInfo archiveFileInfo);
        Stream GetStream(ArchiveSimpleFileInfo archiveSimpleFileInfo);
        IList<(ArchiveFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, Func<string, bool> isSupportedExtension, CancellationToken cancelToken);
        bool IsArchiveFile(string? fullName);
    }
}
