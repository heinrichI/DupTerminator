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
        IEnumerable<ArchiveFileInfo> GetInfoFromArchive(string path, ExtendedFileInfo container, CancellationToken token);

        IEnumerable<ArchiveFileInfo> GetInfoFromArchive(Stream stream, ExtendedFileInfo container);

        bool IsArchiveFile(string? fullName);
    }
}
