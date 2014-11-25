using System;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Threading;
using System.Data.SQLite;
//using SQLite;

namespace DupTerminator
{
    public class DBManager
    {
        private String _dbPath = Path.Combine(System.Windows.Forms.Application.StartupPath, "database.db3");
        //private String _sqliteConnection;
        private SQLiteConnection _sqliteConnection;
        private bool _stopDeleting;

        public delegate void ProgressChangedDelegate(int count);
        public event ProgressChangedDelegate ProgressChangedEvent;

        public delegate void DeletingCompletedDelegate();
        public event DeletingCompletedDelegate DeletingCompletedEvent;

        public delegate void SetMaxValueDelegate(int value);
        public event SetMaxValueDelegate SetMaxValueEvent;

        public DBManager()
        {
            _sqliteConnection = new SQLiteConnection(String.Format("Data Source={0};Version=3;journal mode=memory;cache_size=100000", _dbPath));
            //_sqliteConnection = new SQLiteConnection("Data Source=database.db3;Version=3;Compress=True;");
            /*_sqliteConnection.Open();
            SQLiteCommand sqliteCommand = new SQLiteCommand("PRAGMA journal_mode = MEMORY",_sqliteConnection);
            sqliteCommand.ExecuteNonQuery();*/
            //sqliteCommand.CommandText = "PRAGMA synchronous = OFF";
            //sqliteCommand.ExecuteNonQuery();
            //sqliteCommand.Dispose();
        }

        ~DBManager()
        {
            /*if (_sqliteConnection != null)
                if (_sqliteConnection.State == ConnectionState.Open)
                    _sqliteConnection.Close();*/
        }

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

        public void CreateDataBase()
        {
            if (!File.Exists(_dbPath))
                SQLiteConnection.CreateFile(_dbPath);

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            string sql = @"CREATE TABLE IF NOT EXISTS 
                           ExtendedFileInfo (path           TEXT NOT NULL,
                                             lastWriteTime	TEXT NOT NULL,
	                                         length	        INTEGER NOT NULL, 
                                             md5            TEXT,
                            PRIMARY KEY(Path,LastWriteTime,Length))";

            SQLiteCommand command = new SQLiteCommand(sql, _sqliteConnection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                new CrashReport(ex).ShowDialog();
            }
            _sqliteConnection.Close();
        }

        /*public void Load()
        {
            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();
            SQLiteCommand sqliteCommand = new SQLiteCommand(_sqliteConnection);
            DataTable dt = new DataTable();
            try
            {
                sqliteCommand.CommandText = "SELECT * FROM ExtendedFileInfo";
                SQLiteDataReader sqliteReader = sqliteCommand.ExecuteReader();
                dt.Load(sqliteReader);
                sqliteReader.Close();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                new CrashReport(ex).ShowDialog();
            }
            _sqliteConnection.Close();
        }*/

        public string SizeDB()
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

            if (path == null || lastWriteTime == null || length == null)
                new CrashReport("path == null || lastWriteTim == null || length == null").ShowDialog();

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            String SQLInsert = "INSERT OR REPLACE INTO ExtendedFileInfo(path, lastWriteTime, length, md5) VALUES(?, ?, ?, ?) ";
            //String SQLInsert = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLInsert;
            command.Parameters.AddWithValue("path", path);
            command.Parameters.AddWithValue("lastWriteTime", lastWriteTime);
            command.Parameters.AddWithValue("length", length);
            command.Parameters.AddWithValue("md5", md5);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                //MessageBox.Show(ex.Message + '\n' + path, "Error in function Add()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //throw ex;
                new CrashReport(ex).ShowDialog();
            }

            command.Dispose();
            //System.Diagnostics.Debug.WriteLine(String.Format("md5 added for file {0}, lastwrite: {1}, length: {2}", path, lastWriteTime, length));
            _sqliteConnection.Close();
        }

        public void Update(string path, DateTime lastWriteTime, long length, string md5)
        {
            if (path == null || lastWriteTime == null || length == null)
                new CrashReport("path == null || lastWriteTim == null || length == null").ShowDialog();

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            String SQLInsert = "UPDATE ExtendedFileInfo SET md5 = ? WHERE path = ? AND lastWriteTime = ? AND length = ?";
            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLInsert;
            command.Parameters.AddWithValue("path", path);
            command.Parameters.AddWithValue("lastWriteTime", lastWriteTime);
            command.Parameters.AddWithValue("length", length);
            command.Parameters.AddWithValue("md5", md5);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                //MessageBox.Show(ex.Message + '\n' + path, "Error in function Update()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //throw ex;
                new CrashReport(ex).ShowDialog();
            }

            command.Dispose();
            //System.Diagnostics.Debug.WriteLine(String.Format("md5 updated for file {0}, lastwrite: {1}, length: {2}", path, lastWriteTime, length));
            _sqliteConnection.Close();
        }

        public DataTable ReadAll()
        {
            String SQLSelect = "SELECT * FROM ExtendedFileInfo";
            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLSelect;

            DataTable dt = new DataTable();
            SQLiteDataAdapter da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            //da.Dispose();

            _sqliteConnection.Close();

            return dt;
        }



        public string ReadMD5(string fullName, DateTime lastWriteTime, long length)
        {
            System.Diagnostics.Debug.Assert(Active = true);
            //System.Diagnostics.Debug.WriteLine("Active " + Active + ", ReadMD5(" + fullName );

            string md5 = String.Empty;
            String SQLSelect = @"SELECT * FROM ExtendedFileInfo WHERE Path = ? AND 
                                 LastWriteTime = ? AND
                                 Length = ?";
            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            //string format_date = "yyyy-MM-dd HH:mm:ss.fff";
            //lastWriteTime.ToString(format_date);

            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLSelect;
            command.Parameters.AddWithValue("Path", fullName);
            //command.Parameters.AddWithValue("LastWriteTime", lastWriteTime.ToString(format_date));
            command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
            command.Parameters.AddWithValue("Length", length);

            SQLiteDataReader reader;
            try
            {
                reader = command.ExecuteReader(); 
                while (reader.Read())
                {
                    //Console.WriteLine("Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                    //System.Diagnostics.Debug.WriteLine("Read md5 Path: " + reader["path"] + "\tLastWriteTime: " + reader["LastWriteTime"] + "\tmd5: " + reader["md5"]);
                    md5 = reader["md5"].ToString();
                }
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                //MessageBox.Show(ex.Message + '\n' + fullName, "Error in function ReadMD5()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                new CrashReport(ex).ShowDialog();
            }

            command.Dispose();

            //int u = reader.FieldCount;
            //reader.NextResult();
            //md5 = reader["md5"].ToString();

            //object[] resValues = new object[1];
            //reader.Read();
            //reader.GetValues(resValues);
            //md5 = reader["md5"].ToString();
            //md5 = reader.GetString(0);
 
            _sqliteConnection.Close();

            return md5;
        }

        public void Delete(string fullName, DateTime lastWriteTime, long length)
        {
            String SQLDelete = "DELETE FROM ExtendedFileInfo WHERE path = ? AND lastWriteTime = ? AND length = ?";

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLDelete;
            command.Parameters.AddWithValue("Path", fullName);
            command.Parameters.AddWithValue("LastWriteTime", lastWriteTime);
            command.Parameters.AddWithValue("Length", length);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                new CrashReport(ex).ShowDialog();
            }
            _sqliteConnection.Close();
        }

        public void Delete(string fullName)
        {
            String SQLDelete = "DELETE FROM ExtendedFileInfo WHERE path = ?";

            if (_sqliteConnection.State != ConnectionState.Open)
                _sqliteConnection.Open();

            SQLiteCommand command = _sqliteConnection.CreateCommand();
            command.CommandText = SQLDelete;
            command.Parameters.AddWithValue("Path", fullName);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _sqliteConnection.Close();
                new CrashReport(ex).ShowDialog();
            }

            _sqliteConnection.Close();
        }

        public void DeleteDB()
        {
            if (_sqliteConnection.State == ConnectionState.Open)
            {
                _sqliteConnection.Close();
                _sqliteConnection.Dispose();
            }
            GC.Collect();
           
            //Thread.Sleep(1000);
            File.Delete(_dbPath);
        }

        public void CleanDB()
        {
            _stopDeleting = false;
            Thread thFileDel = new Thread(Deleting);
            thFileDel.Name = "DupTerminator: Deleting";

            //Start the file search and check on a new thread.
            thFileDel.Start();
        }

        private void Deleting()
        {
            uint deleted = 0;
            DataTable dt;
            dt = ReadAll();
            int rowsCount = dt.Rows.Count;
            SetMaxValueEvent(rowsCount-1);
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

            MessageBox.Show(String.Format(LanguageManager.GetString("OutdateRecordDel"), deleted));

            Vacuum();

            _sqliteConnection.Close();

            DeletingCompletedEvent();
        }

        public void CancelDeleting()
        {
            _stopDeleting = true;
        }

        public void Vacuum()
        {
            try
            {
                if (_sqliteConnection.State != ConnectionState.Open)
                    _sqliteConnection.Open();
                using (SQLiteCommand cmd = _sqliteConnection.CreateCommand())
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
