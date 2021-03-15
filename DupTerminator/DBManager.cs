using System;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Threading;
using System.Data.SQLite;
using DupTerminator.Views;
//using SQLite;

namespace DupTerminator
{
    public class DBManager
    {
        private const string sqlConnectionFile = "Data Source={0}";
        //private const string sqlConnectionMemory = "Data Source=:memory:;Version=3;New=True;";
        private const string sqlConnectionMemory = "Data Source=:memory:";
        private const string sqlCreate = @"CREATE TABLE IF NOT EXISTS 
                           ExtendedFileInfo (path           TEXT NOT NULL,
                                             lastWriteTime	TEXT NOT NULL,
	                                         length	        INTEGER NOT NULL, 
                                             md5            TEXT,
                            PRIMARY KEY(Path,LastWriteTime,Length))";
        private readonly string SQLUpdate = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
        private const string SQLInsert = "INSERT OR REPLACE INTO ExtendedFileInfo(path, lastWriteTime, length, md5) VALUES(?, ?, ?, ?) ";
        private const string SQLSelectAll = "SELECT * FROM ExtendedFileInfo";
        private readonly string SQLSelect = @"SELECT * FROM ExtendedFileInfo WHERE Path = ? AND 
                                 LastWriteTime = ? AND
                                 Length = ?";
        private const string SQLDelete = "DELETE FROM ExtendedFileInfo WHERE path = ? AND lastWriteTime = ? AND length = ?";
        private const string SQLDeletePath = "DELETE FROM ExtendedFileInfo WHERE path = ?";
        
        private String _dbPath = Path.Combine(System.Windows.Forms.Application.StartupPath, "database.db3");

        /// <summary>
        /// This is the one instance of this type.
        /// </summary>
        private static volatile DBManager singletonInstance;
        private static readonly Object syncRoot = new Object();

        //private String _sqliteConnection;
        //private SQLiteConnection _sqliteConnectionFile;
        private SQLiteConnection _sqliteConnectionMemory;
        private SQLiteCommand _command;
        //private SQLiteCommand _commandRead;
        private bool _stopDeleting;

        public delegate void ProgressChangedDelegate(int count);
        public event ProgressChangedDelegate ProgressChangedEvent;

        public delegate void DeletingCompletedDelegate();
        public event DeletingCompletedDelegate DeletingCompletedEvent;

        public delegate void SetMaxValueDelegate(int value);
        public event SetMaxValueDelegate SetMaxValueEvent;

        // Private constructor allowing this type to construct the Singleton.
        private DBManager() 
        {
            _sqliteConnectionMemory = new SQLiteConnection(sqlConnectionMemory);
            _command = _sqliteConnectionMemory.CreateCommand();
        }

        /// <summary>
        /// A method returning a reference to the Singleton.
        /// </summary>
        public static DBManager GetInstance()
        {
            // создан ли объект
            if (singletonInstance == null)
            {
                // нет, не создан
                // только один поток может создать его
                lock (syncRoot)
                {
                    // проверяем, не создал ли объект другой поток
                    if (singletonInstance == null)
                    {
                        // нет не создал — создаём
                        singletonInstance = new DBManager();
                    }
                }
            }
            return singletonInstance;
        }

        ~DBManager()
        {
            /*if (_sqliteConnectionFile != null)
            {
                //SaveFromMemory();

                if (_sqliteConnectionFile.State == ConnectionState.Open)
                    _sqliteConnectionFile.Close();
            }*/
            /*if (_sqliteConnectionMemory != null)
            {
                if (_sqliteConnectionMemory.State == ConnectionState.Open)
                    _sqliteConnectionMemory.Close();
                /*System.Diagnostics.Debug.Assert(_sqliteConnectionMemory.State == ConnectionState.Open);

                using (SQLiteConnection sqliteConnectionFile = new SQLiteConnection(String.Format(sqlConnectionFile, _dbPath)))
                {
                    sqliteConnectionFile.Open();

                    if (_sqliteConnectionMemory.State != ConnectionState.Open)
                        _sqliteConnectionMemory.Open();

                    // save memory db to file
                    _sqliteConnectionMemory.BackupDatabase(sqliteConnectionFile, "main", "main", -1, null, 0);
                    _sqliteConnectionMemory.Close();
                }
            }*/
        }

        public void SaveFromMemory()
        {
            if (_sqliteConnectionMemory.State == ConnectionState.Open)
            {
                //    new CrashReport("sqliteConnectionMemory.State != ConnectionState.Open").ShowDialog();
                System.Diagnostics.Debug.Assert(_sqliteConnectionMemory.State == ConnectionState.Open);

                using (SQLiteConnection sqliteConnectionFile = new SQLiteConnection(String.Format(sqlConnectionFile, _dbPath)))
                {
                    sqliteConnectionFile.Open();

                    //FormProgress _formProgress = new FormProgress();
                    //_formProgress.Icon = Properties.Resources.SettingIco;
                    //SQLiteBackupCallback(_formProgress.BackupEventHandler);
                    //_formProgress.Show();
                    //SQLiteBackupCallback BackupDelegate = new SQLiteBackupCallback(_formProgress.BackupEventHandler);
                    //BackupDelegate = new SQLiteBackupCallback(BackupEventHandler);
                    //SQLiteBackupCallback BackupDelegate = new SQLiteBackupCallback(BackupEventHandler);
                    //BackupDelegate += new SQLiteBackupCallback(BackupEventHandler);
                    //_formProgress. new DBManager.ProgressChangedDelegate(ProgressChangedEventHandler)

                    /*_sqliteConnectionMemory.Trace += new SQLiteTraceEventHandler(delegate(object sender, TraceEventArgs e)
                    {
                        MessageBox.Show("Trace");
                    });
                    _sqliteConnectionMemory.Update += new SQLiteUpdateEventHandler(delegate(object sender, UpdateEventArgs e)
                    {
                        MessageBox.Show("SQLiteUpdateEventHandler");
                    });
                    sqliteConnectionFile.Update += new SQLiteUpdateEventHandler(delegate(object sender, UpdateEventArgs e)
                    {
                        MessageBox.Show("sqliteConnectionFile SQLiteUpdateEventHandler");
                    });*/
                    //_sqliteConnectionMemory.BackupDatabase(sqliteConnectionFile, "main", "main", -1, _formProgress.BackupEventHandler, 10);
                    // save memory db to file
                    _sqliteConnectionMemory.BackupDatabase(sqliteConnectionFile, "main", "main", -1, null, 0);
                    _sqliteConnectionMemory.Close();
                }
            }
        }

        /*SQLiteBackupCallback BackupDelegate;
        public bool BackupEventHandler(SQLiteConnection source, string sourceName, SQLiteConnection destination, string destinationName, int pages, int remainingPages, int totalPages, bool retry)
        {
            MessageBox.Show("OK");
            return true;
        }*/

        /*public void CreateDataBase()
        {
            _sqliteConnection = String.Format("Data Source={0}", _dbPath);
            var db = new SQLiteConnection(_dbPath);
            db.BeginTransaction();
            db.CreateTable<ExtendedFileInfo>();
            //db.Execute("CREATE INDEX if not exists \"main\".\"ix_DirectoryInformation_driveid_path\" ON \"DirectoryInformation\" (\"DriveId\" ASC, \"Path\" ASC)");
            db.Commit(); 
        }//*/

        public bool Active
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Создание базы данных на диске, если уже не существует.
        /// </summary>
        public void CreateDataBase()
        {
            if (!File.Exists(_dbPath))
                SQLiteConnection.CreateFile(_dbPath);

            using (SQLiteConnection sqliteConnectionFile = new SQLiteConnection(String.Format(sqlConnectionFile, _dbPath)))
            {
                if (sqliteConnectionFile.State != ConnectionState.Open)
                    sqliteConnectionFile.Open();

                SQLiteCommand command = new SQLiteCommand(sqlCreate, sqliteConnectionFile);
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    sqliteConnectionFile.Close();
                    new CrashReport(ex).ShowDialog();
                }

                if (_sqliteConnectionMemory.State != ConnectionState.Open)
                    _sqliteConnectionMemory.Open();

                // copy db file to memory
                sqliteConnectionFile.BackupDatabase(_sqliteConnectionMemory, "main", "main", -1, null, 0);
                sqliteConnectionFile.Close();
            }
        }

        /// <summary>
        /// Загрузка базы данных в память.
        /// </summary>
        public void LoadToMemory()
        {
            using (SQLiteConnection sqliteConnectionFile = new SQLiteConnection(String.Format(sqlConnectionFile, _dbPath)))
            {
                sqliteConnectionFile.Open();

                if (_sqliteConnectionMemory.State != ConnectionState.Open)
                    _sqliteConnectionMemory.Open();

                // copy db file to memory
                sqliteConnectionFile.BackupDatabase(_sqliteConnectionMemory, "main", "main", -1, null, 0);
                sqliteConnectionFile.Close();
            }
        }

        public string GetSizeDB()
        {
            string size;
            if (File.Exists(_dbPath))
                size = (new FileInfo(_dbPath).Length / 1024).ToString() + " Kb";
            else
                size = "0 Kb";
            return size;
        }

        public void Add(string path, DateTime lastWriteTime, long length, string md5)
        {
            System.Diagnostics.Debug.Assert(Active = true);

            if (path == null || lastWriteTime == null)
                new CrashReport("path == null || lastWriteTim == null").ShowDialog();

            CheckMemoryState();

            //String SQLInsert = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            _command.CommandText = SQLInsert;
            _command.Parameters.AddWithValue("path", path);
            _command.Parameters.AddWithValue("lastWriteTime", lastWriteTime);
            _command.Parameters.AddWithValue("length", length);
            _command.Parameters.AddWithValue("md5", md5);

            try
            {
                _command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnectionMemory.Close();
                //MessageBox.Show(ex.Message + '\n' + path, "Error in function Add()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                new CrashReport(ex).ShowDialog();
            }
            //command.Dispose();
            //System.Diagnostics.Debug.WriteLine(String.Format("md5 added for file {0}, lastwrite: {1}, length: {2}", path, lastWriteTime, length));
        }

        /*public void Update(string path, DateTime lastWriteTime, long length, string md5)
        {
            if (path == null || lastWriteTime == null)
                new CrashReport("path == null || lastWriteTime == null").ShowDialog();

            System.Diagnostics.Debug.Assert(_sqliteConnectionMemory.State == ConnectionState.Open);
           //if (_sqliteConnectionMemory.State != ConnectionState.Open)
           //     _sqliteConnectionMemory.Open();

           try
            {
                //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
                //command.CommandText = SQLUpdate;
                //command.Parameters.AddWithValue("path", path);
                //command.Parameters.AddWithValue("lastWriteTime", lastWriteTime);
                //command.Parameters.AddWithValue("length", length);
                //command.Parameters.AddWithValue("md5", md5);

                _command.CommandText = SQLUpdate;
                _command.Parameters.AddWithValue("path", path);
                _command.Parameters.AddWithValue("lastWriteTime", lastWriteTime);
                _command.Parameters.AddWithValue("length", length);
                _command.Parameters.AddWithValue("md5", md5);

                //_command.ExecuteNonQuery();
                //System.Diagnostics.Debug.Assert(_command.ExecuteNonQuery() == 1);
                int res = _command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnectionMemory.Close();
                //MessageBox.Show(ex.Message + '\n' + path, "Error in function Update()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                new CrashReport(ex).ShowDialog();
            }
            //command.Dispose();
            //System.Diagnostics.Debug.WriteLine(String.Format("md5 updated for file {0}, lastwrite: {1}, length: {2}", path, lastWriteTime, length));
            //_sqliteConnectionMemory.Close();
        }*/

        /*public DataTable ReadAll(out DataTable dt)
        {
            //if (_sqliteConnectionMemory.State != ConnectionState.Open)
            //    _sqliteConnectionMemory.Open();

            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            _command.CommandText = SQLSelectAll;

            //DataTable dt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(_command);
            da.Fill(dt);

            //da.Dispose();

            //_sqliteConnectionMemory.Close();

            return dt;
        }*/

        public string ReadMD5(string fullName, DateTime lastWriteTime, long length)
        {
            System.Diagnostics.Debug.Assert(Active = true);

            CheckMemoryState();

            string md5 = String.Empty;

            using (SQLiteCommand command = _sqliteConnectionMemory.CreateCommand())
            {
                try
                {
                    command.CommandText = SQLSelect;
                    command.Parameters.AddWithValue("Path", fullName);
                    //command.Parameters.AddWithValue("LastWriteTime", lastWriteTime.ToString(format_date));
                    command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
                    command.Parameters.AddWithValue("Length", length);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Console.WriteLine("Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                            //System.Diagnostics.Debug.WriteLine("Read md5 Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                            md5 = reader["md5"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _sqliteConnectionMemory.Close();
                    //MessageBox.Show(ex.Message + '\n' + fullName, "Error in function ReadMD5()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    new CrashReport(ex).ShowDialog();
                }
            }

            return md5;
        }

        public void Delete(string fullName, DateTime lastWriteTime, long length)
        {
            CheckMemoryState();

            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            _command.CommandText = SQLDelete;
            _command.Parameters.AddWithValue("Path", fullName);
            _command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
            _command.Parameters.AddWithValue("Length", length);

            try
            {
                _command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnectionMemory.Close();
                new CrashReport(ex).ShowDialog();
            }
            //_sqliteConnectionMemory.Close();
        }

        public void Delete(string fullName)
        {
            CheckMemoryState();
            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            _command.CommandText = SQLDeletePath;
            _command.Parameters.AddWithValue("Path", fullName);
            try
            {
                _command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnectionMemory.Close();
                new CrashReport(ex).ShowDialog();
            }
            //_sqliteConnectionMemory.Close();
        }

        public void DeleteDB()
        {
            if (_sqliteConnectionMemory.State == ConnectionState.Open)
            {
                _sqliteConnectionMemory.Close();
                _sqliteConnectionMemory.Dispose();
            }

            GC.Collect();

            Thread.Sleep(1000);

            File.Delete(_dbPath);
        }


        /// <summary>
        /// Проверка того что база данных в памяти и открыта.
        /// </summary>
        private void CheckMemoryState()
        {
            if (_sqliteConnectionMemory.State != ConnectionState.Open)
            {
                //MessageBox.Show("SqliteConnectionMemory not open!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadToMemory();
            }
        }

        /// <summary>
        /// Очистка базы данных от устаревших записей.
        /// </summary>
        public void CleanDB()
        {


            CheckMemoryState();

            _stopDeleting = false;
            Thread thFileDel = new Thread(Deleting);
            thFileDel.Name = "DupTerminator: Deleting";

            //Start the file search and check on a new thread.
            thFileDel.Start();
        }

        /// <summary>
        /// Удаление устаревших записей.
        /// </summary>
        private void Deleting()
        {
            uint deleted = 0;
            using (DataTable dt = new DataTable())
            {
                _command.CommandText = SQLSelectAll;
                SQLiteDataAdapter da = new SQLiteDataAdapter(_command);
                da.Fill(dt);

                int rowsCount = dt.Rows.Count;
                if (rowsCount > 0)
                {
                    SetMaxValueEvent(rowsCount - 1);
                    for (int i = 0; i < rowsCount; i++)
                    {
                        string path = dt.Rows[i]["path"].ToString();
                        if (File.Exists(path))
                        {
                            DateTime lastWrite;
                            lastWrite = DateTime.Parse(dt.Rows[i]["lastWriteTime"].ToString());
                            long length = long.Parse(dt.Rows[i]["length"].ToString());
                            FileInfo fi = new FileInfo(path);
                            if (fi.LastWriteTime != lastWrite ||
                                fi.Length != length)
                            {
                                Delete(path, lastWrite, length);
                                deleted++;
                            }
                        }
                        else
                        {
                            Delete(path);
                            deleted++;
                        }

                        ProgressChangedEvent(i);

                        if (_stopDeleting)
                            break;
                    }
                }
            }

            Vacuum();

            SaveFromMemory();

            MessageBox.Show(String.Format(LanguageManager.GetString("OutdateRecordDel"), deleted));

            DeletingCompletedEvent();
        }

        public void CancelDeletingEventHandler()
        {
            _stopDeleting = true;
        }

        public void Vacuum()
        {
            try
            {
                using (SQLiteCommand cmd = _sqliteConnectionMemory.CreateCommand())
                {
                    cmd.CommandText = "VACUUM";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error in function Vacuum()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                new CrashReport(ex).ShowDialog();
            }
        }


        private System.Data.SQLite.SQLiteTransaction tr;
        public void BeginInsert()
        {
            if (_sqliteConnectionMemory.State != ConnectionState.Open)
                _sqliteConnectionMemory.Open();
            tr = _sqliteConnectionMemory.BeginTransaction();
        }

        public void EndInsert()
        {
            tr.Commit();
            //_sqliteConnectionMemory.Close();
        }
    }

    /*public DataTable GetDataTable(string sql)
    {
       DataTable dt = new DataTable();
        try
	     {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
	            cnn.Open();
	            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
	            SQLiteDataReader reader = mycommand.ExecuteReader();
            dt.Load(reader);
            reader.Close();
	            cnn.Close();
	        }
	        catch (Exception e)
        {
            throw new Exception(e.Message);
	        }
        return dt;
	    }*/
}
