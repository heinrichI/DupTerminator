using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Data.Sqlite;
//using SQLite;

namespace DupTerminator.DataBase
{
    class DBManager : IDBManager
    {
        private const string SQL_CONNECTION_FILE = "Data Source=database.db;";
        //private const string sqlConnectionMemory = "Data Source=:memory:;Version=3;New=True;";
        //private const string SQL_CONNECTION_MEMORY = "Data Source=:memory:";
        private const string SQL_CREATE = @" PRAGMA synchronous = OFF;
                            PRAGMA journal_mode = OFF;
                            CREATE TABLE IF NOT EXISTS 
                            ExtendedFileInfo (Path           TEXT NOT NULL,
                                             LastWriteTime	TEXT NOT NULL,
	                                         Size	        INTEGER NOT NULL, 
                                             Md5            TEXT,
                            PRIMARY KEY(Path,LastWriteTime,Size))";
        private const string SQL_UPDATE = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
        private const string SQL_SELECT_ALL = "SELECT * FROM ExtendedFileInfo";
        private const string SQL_DELETE = "DELETE FROM ExtendedFileInfo WHERE path = ? AND lastWriteTime = ? AND length = ?";
        private const string SQL_DELETE_PATH = "DELETE FROM ExtendedFileInfo WHERE path = ?";

        // Create a composite index on 'Column1' and 'Column2' of 'YourTable'
        //private const string CREATE_INDEX_SQL = "CREATE INDEX IF NOT EXISTS idx_ExtendedFileInfo_Columns ON YourTable (path, Column2);";

        private readonly IMessageService _messageService;

        //private String _sqliteConnection;
        //private SQLiteConnection _sqliteConnectionFile;
        //private SqliteConnection _sqliteConnection;
        //private SQLiteCommand _commandRead;
        private bool _stopDeleting;

        public event ProgressChangedDelegate ProgressChangedEvent;

        public event DeletingCompletedDelegate DeletingCompletedEvent;

        public event SetMaxValueDelegate SetMaxValueEvent;

        // Private constructor allowing this type to construct the Singleton.
        public DBManager(IMessageService messageService)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            //_sqliteConnection = new SqliteConnection(SQL_CONNECTION_FILE);
            //_command = _sqliteConnectionMemory.CreateCommand();

            CreateDataBase();
        }

        private void CreateDataBase()
        {
            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
            connection.Open();

            using var createTable = connection.CreateCommand();
            createTable.CommandText = SQL_CREATE;
            createTable.ExecuteNonQuery();
        }

        /// <summary>
        /// Создание базы данных на диске, если уже не существует.
        /// </summary>
        //public void CreateDataBase()
        //{
        //    using (SqliteConnection sqliteConnectionFile = new SqliteConnection(String.Format(SQL_CONNECTION_FILE)))
        //    {
        //        if (sqliteConnectionFile.State != ConnectionState.Open)
        //            sqliteConnectionFile.Open();

        //        using (SqliteCommand command = new SqliteCommand(SQL_CREATE, sqliteConnectionFile))
        //            command.ExecuteNonQuery();

        //        if (_sqliteConnection.State != ConnectionState.Open)
        //            _sqliteConnection.Open();


        //        //using (var command = new SqliteCommand(CREATE_INDEX_SQL, sqliteConnectionFile))
        //        //{
        //        //    command.ExecuteNonQuery();
        //        //}

        //        // copy db file to memory
        //        sqliteConnectionFile.BackupDatabase(_sqliteConnection, "main", "main");
        //        sqliteConnectionFile.Close();
        //    }
        //}

        /// <summary>
        /// Загрузка базы данных в память.
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


        public void Add(string path, DateTime lastWriteTime, ulong size, string md5)
        {
            if (path == null || lastWriteTime == null)
                throw new ArgumentNullException("path == null || lastWriteTim == null");

            CheckMemoryState();

            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
            if (connection.State != ConnectionState.Open)
                connection.Open();


            using (var command = connection.CreateCommand())
            {
                //String SQLInsert = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
                //SQLiteCommand command = _sqliteConnectionMemory.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO ExtendedFileInfo(path, lastWriteTime, size, md5) VALUES(@path, @lastWriteTime, @size, @md5)";
                command.Parameters.AddWithValue("@path", path);
                command.Parameters.AddWithValue("@lastWriteTime", lastWriteTime);
                //command.Parameters.AddWithValue("@containerLastWriteTime", containerLastWriteTime);
                command.Parameters.AddWithValue("@size", size);
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

        public string ReadMD5(string fullName, DateTime lastWriteTime, ulong size)
        {
            CheckMemoryState();

            string md5 = String.Empty;

            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ExtendedFileInfo WHERE Path = @Path AND 
                                 LastWriteTime = @LastWriteTime AND
                                 Size = @Size";
                command.Parameters.AddWithValue("Path", fullName);
                //command.Parameters.AddWithValue("LastWriteTime", lastWriteTime.ToString(format_date));
                command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
                //command.Parameters.AddWithValue("ContainerLastWriteTime", containerLastWriteTime);
                command.Parameters.AddWithValue("Size", size);

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
            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SQL_DELETE;
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
            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SQL_DELETE_PATH;
                command.Parameters.AddWithValue("Path", fullName);
                command.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// Проверка того что база данных в памяти и открыта.
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
                using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SQL_SELECT_ALL;
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

            //SaveFromMemory();

            _messageService.DeletedOutdatedRecord(deleted);

            DeletingCompletedEvent();
        }

        public void CancelDeletingEventHandler()
        {
            _stopDeleting = true;
        }

        public void Vacuum()
        {
            using var connection = new SqliteConnection(SQL_CONNECTION_FILE);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            using (SqliteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "VACUUM";
                cmd.ExecuteNonQuery();
            }
        }


        private SqliteTransaction _tr;
        //public void BeginInsert()
        //{
        //    if (_sqliteConnection.State != ConnectionState.Open)
        //        _sqliteConnection.Open();
        //    _tr = _sqliteConnection.BeginTransaction();
        //}

        public void EndInsert()
        {
            _tr.Commit();
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
