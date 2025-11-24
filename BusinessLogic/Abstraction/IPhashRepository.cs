using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    public interface IPhashRepository
    {
        public ulong? Get(string path, DateTime lastWriteTime, ulong size);

        public void Add(string path, DateTime lastWriteTime, ulong size, ulong phash);
    }
}