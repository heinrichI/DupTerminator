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
        T CalculateHashInArchive<T>(ArchiveFileInfo fileInfo, Func<Stream, T> calculator);

        //IEnumerable<ExtendedFileInfo> GetHashesFromArchive(ExtendedFileInfo fileInfo);
        ArchiveFileInfo[] GetInfoFromArchive(ExtendedFileInfo archive, CancellationToken token, bool archiveInArchive = false);

        IEnumerable<ArchiveFileInfo> GetInfoFromArchive(Stream stream, ExtendedFileInfo container, bool archiveInArchive = false);
        Stream GetStream(ArchiveFileInfo archiveFileInfo);
        IList<(ArchiveFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, Func<string, bool> isSupportedExtension, CancellationToken cancelToken);
        bool IsArchiveFile(string? fullName);
    }
}
