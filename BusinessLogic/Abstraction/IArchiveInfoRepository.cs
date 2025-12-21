using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    public interface IArchiveInfoRepository
    {
        void Add(ExtendedFileInfo container, IEnumerable<ArchiveFileInfo> files);
        //Task<IEnumerable<ExtendedFileInfo>?> Get(string path, DateTime lastWriteTime, ulong size);
        ArchiveFileInfo[] Get(string path, DateTime lastWriteTime, ulong size);
    }
}