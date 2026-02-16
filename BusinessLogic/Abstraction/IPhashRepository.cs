using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    public interface IPhashRepository
    {
        (ulong phash, int width, int height)? Get(string path, DateTime lastWriteTime, ulong size);

        void Add(string path, DateTime lastWriteTime, ulong size, ulong phash, int width, int height);

        (ExtendedFileInfo efi, ulong phash, int width, int height)[] GetContainerHashes(ExtendedFileInfo fileInfo);

        void AddContainerStreams(ExtendedFileInfo fileInfo, (ExtendedFileInfo efi, ulong phash, int width, int height)[] collection);

        void VacuumDatabase();

        string[] GetAllContainerPath();

        int DeleteByPath(string path);
    }
}