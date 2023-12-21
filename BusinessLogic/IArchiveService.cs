using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public interface IArchiveService
    {
        IEnumerable<ExtendedFileInfo> CalculateHashInArchive(string path);
        IEnumerable<ExtendedFileInfo> GetInfoFromArchive(string fullName);
        bool IsArchiveFile(string? fullName);
    }
}
