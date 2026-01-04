using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase
{
    public interface IPdfInfoRepository
    {
        void Add(ExtendedFileInfo container, IEnumerable<PdfFileInfo> files);

        PdfFileInfo[] Get(string path, DateTime lastWriteTime, ulong size);
    }
}