namespace DupTerminator.BusinessLogic.Model
{
    public interface IArchiveItem
    {
        bool ArchiveInArchive { get; }
        DateTime GetContainerLastWriteTime();
    }
}
