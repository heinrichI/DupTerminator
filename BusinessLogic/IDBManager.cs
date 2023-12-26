
namespace DupTerminator.BusinessLogic
{
    public delegate void ProgressChangedDelegate(int count);
    public delegate void SetMaxValueDelegate(int value);
    public delegate void DeletingCompletedDelegate();

    public interface IDBManager
    {
        event DeletingCompletedDelegate DeletingCompletedEvent;
        event ProgressChangedDelegate ProgressChangedEvent;
        event SetMaxValueDelegate SetMaxValueEvent;

        void Add(string path, DateTime lastWriteTime, long length, string md5);
        void BeginInsert();
        void CancelDeletingEventHandler();
        void CleanDB();
        void CreateDataBase();
        void Delete(string fullName);
        void Delete(string fullName, DateTime lastWriteTime, long length);
        void DeleteDB();
        void EndInsert();
        string GetSizeDB();
        //void LoadToMemory();
        string ReadMD5(string fullName, DateTime lastWriteTime, long length);
        void SaveFromMemory();
        void Vacuum();
    }
}