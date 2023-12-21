using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public interface IArchiveService
    {
        string? CalculateHashInArchive(ExtendedFileInfo fileInfo);
        //IEnumerable<ExtendedFileInfo> GetHashesFromArchive(ExtendedFileInfo fileInfo);
        IEnumerable<ExtendedFileInfo> GetInfoFromArchive(string path, ExtendedFileInfo container, CancellationToken token);
        bool IsArchiveFile(string? fullName);
    }
}
