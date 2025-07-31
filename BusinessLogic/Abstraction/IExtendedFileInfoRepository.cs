using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    public interface IExtendedFileInfoRepository
    {
        void Add(ExtendedFileInfo container, IEnumerable<ArchiveFileInfo> files);
        //Task<IEnumerable<ExtendedFileInfo>?> Get(string path, DateTime lastWriteTime, ulong size);
        IEnumerable<ArchiveFileInfo>? Get(string path, DateTime lastWriteTime, ulong size);
    }
}