using System;
using System.IO;
using System.Data;
using System.Threading;
using DupTerminator.BusinessLogic;
using Microsoft.Data.Sqlite;
//using SQLite;

namespace DupTerminator.DataBase
{
    public class DBManager : IDBManager
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
        private const string SQLSelectAll = "SELECT * FROM ExtendedFileInfo";
        private const string SQLDelete = "DELETE FROM ExtendedFileInfo WHERE path = ? AND lastWriteTime = ? AND length = ?";
        private const string SQLDeletePath = "DELETE FROM ExtendedFileInfo WHERE path = ?";

        private readonly string _dbPath;
        private readonly IMessageService _messageService;


        /// <summary>
        /// This is the one instance of this type.
        /// </summary>
        private static volatile DBManager singletonInstance;
        private static readonly Object syncRoot = new Object();

        //private String _sqliteConnection;
        //private SQLiteConnection _sqliteConnectionFile;
        private SqliteConnection _sqliteConnection;
        //private SQLiteCommand _commandRead;
        private bool _stopDeleting;

        public event ProgressChangedDelegate ProgressChangedEvent;

        public event DeletingCompletedDelegate DeletingCompletedEvent;

        public event SetMaxValueDelegate SetMaxValueEvent;

        // Private constructor allowing this type to construct the Singleton.
        public DBManager(string dbPath, IMessageService messageService)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            _sqliteConnection = new SqliteConnection(sqlConnectionMemory);
            //_command = _sqliteConnectionMemory.CreateCommand();

            CreateDataBase();
        }

        public void SaveFromMemory()
        {
            if (_sqliteConnection.State == ConnectionState.Open)
            {
                //    new CrashReport("sqliteConnectionMemory.State != ConnectionState.Open").ShowDialog();
                System.Diagnostics.Debug.Assert(_sqliteConnection.State == ConnectionState.Open);

                using (SqliteConnection sqliteConnectionFile = new SqliteConnection(String.Format(sqlConnectionFile, _dbPath)))
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
                    _sqliteConnection.BackupDatabase(sqliteConnectionFile, "main", "main");
                    _sqliteConnection.Close();
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
        /// »F»d»]»Y»U»c»^»Z »V»U»]»q »Y»U»c»c»q»k »c»U »Y»^»g»`»Z, »Z»g»a»^ »i»\»Z »c»Z »g»i»o»Z»g»h»W»i»Z»h.
        /// </summary>
        public void CreateDataBase()
        {
            using (SqliteConnection sqliteConnectionFile = new SqliteConnection(String.Format(sqlConnectionFile, _dbPath)))
            {
                if (sqliteConnectionFile.State != ConnectionState.Open)
                    sqliteConnectionFile.Open();

                SqliteCommand command = new SqliteCommand(sqlCreate, sqliteConnectionFile);
                command.ExecuteNonQuery();

                if (_sqliteConnection.State != ConnectionState.Open)
                    _sqliteConnection.Open();

                // copy db file to memory
                sqliteConnectionFile.BackupDatabase(_sqliteConnection, "main", "main");
                sqliteConnectionFile.Close();
            }
        }

        /// <summary>
        /// «˚»U»X»f»i»]»`»U »V»U»]»q »Y»U»c»c»q»k »W »e»U»b»u»h»r.
        /// </summary>
        //public void LoadToMemory()
        //{
        //    using (var sqliteConnectionFile = new SqliteCommand(String.Format(sqlConnectionFile, _dbPath)))
        //    {
        //        sqliteConnectionFile.Open();

        //        if (_sqliteConnectionMemory.State != ConnectionState.Open)
        //            _sqliteConnectionMemory.Open();

        //        // copy db file to memory
        //        sqliteConnectionFile.BackupDatabase(_sqliteConnectionMemory, "main", "main", -1, null, 0);
        //        sqliteConnectionFile.Close();
        //    }
        //}

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
                throw new ArgumentNullException("path == null || lastWriteTim == null");

            CheckMemoryState();


            using (var command = _sqliteConnection.CreateCommand())
            {
                //String SQLInsert = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
                //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO ExtendedFileInfo(path, lastWriteTime, length, md5) VALUES(@path, @lastWriteTime, @length, @md5)";
                command.Parameters.AddWithValue("@path", path);
                command.Parameters.AddWithValue("@lastWriteTime", lastWriteTime);
                command.Parameters.AddWithValue("@length", length);
                command.Parameters.AddWithValue("@md5", md5);
                command.Prepare();

                int updated = command.ExecuteNonQuery();

                //command.Dispose();
            }
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

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            using (SqliteCommand command = _sqliteConnection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ExtendedFileInfo WHERE Path = @Path AND 
                                 LastWriteTime = @LastWriteTime AND
                                 Length = @Length";
                command.Parameters.AddWithValue("Path", fullName);
                //command.Parameters.AddWithValue("LastWriteTime", lastWriteTime.ToString(format_date));
                command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
                command.Parameters.AddWithValue("Length", length);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                        //System.Diagnostics.Debug.WriteLine("Read md5 Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                        md5 = reader["md5"].ToString();
                    }
                }
            }

            return md5;
        }

        public void Delete(string fullName, DateTime lastWriteTime, long length)
        {
            CheckMemoryState();

            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            using (var command = _sqliteConnection.CreateCommand())
            {
                command.CommandText = SQLDelete;
                command.Parameters.AddWithValue("Path", fullName);
                command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
                command.Parameters.AddWithValue("Length", length);

                command.ExecuteNonQuery();
            }
        }

        public void Delete(string fullName)
        {
            CheckMemoryState();
            //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
            using (var command = _sqliteConnection.CreateCommand())
            {
                command.CommandText = SQLDeletePath;
                command.Parameters.AddWithValue("Path", fullName);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteDB()
        {
            if (_sqliteConnection.State == ConnectionState.Open)
            {
                _sqliteConnection.Close();
                _sqliteConnection.Dispose();
            }

            GC.Collect();

            Thread.Sleep(1000);

            File.Delete(_dbPath);
        }


        /// <summary>
        /// »D»f»d»W»Z»f»`»U »h»d»X»d »m»h»d »V»U»]»U »Y»U»c»c»q»k »W »e»U»b»u»h»^ »^ »d»h»`»f»q»h»U.
        /// </summary>
        private void CheckMemoryState()
        {
            //if (_sqliteConnectionMemory.State != ConnectionState.Open)
            //{
            //    //MessageBox.Show("SqliteConnectionMemory not open!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    LoadToMemory();
            //}
        }

        /// <summary>
        /// »C»m»^»g»h»`»U »V»U»]»q »Y»U»c»c»q»k »d»h »i»g»h»U»f»Z»W»n»^»k »]»U»e»^»g»Z»_.
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
        /// »H»Y»U»a»Z»c»^»Z »i»g»h»U»f»Z»W»n»^»k »]»U»e»^»g»Z»_.
        /// </summary>
        private void Deleting()
        {
            uint deleted = 0;
            using (DataTable dt = new DataTable())
            {
                using (var command = _sqliteConnection.CreateCommand())
                {
                    command.CommandText = SQLSelectAll;
                    //SQLiteDataAdapter da = new SQLiteDataAdapter(_command);
                    //da.Fill(dt);

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
            }

            Vacuum();

            SaveFromMemory();

            _messageService.DeletedOutdatedRecord(deleted);

            DeletingCompletedEvent();
        }

        public void CancelDeletingEventHandler()
        {
            _stopDeleting = true;
        }

        public void Vacuum()
        {
            using (SqliteCommand cmd = _sqliteConnection.CreateCommand())
            {
                cmd.CommandText = "VACUUM";
                cmd.ExecuteNonQuery();
            }
        }


        private SqliteTransaction tr;
        public void BeginInsert()
        {
            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();
            tr = _sqliteConnection.BeginTransaction();
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
