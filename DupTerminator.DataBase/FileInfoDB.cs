//using System;
//using System.Collections.Generic;
//using System.Data.SQLite;
//using DupTerminator.BusinessLogic;
//using Microsoft.Data.Sqlite;

//namespace DupTerminator.DataBase
//{
//    public class FileInfoDB
//    {
//        private readonly string _connectionString;

//        public FileInfoDB(string dbPath)
//        {
//            _connectionString = $"Data Source={dbPath};Version=3;";
//            InitializeDatabase();
//        }

//        private void InitializeDatabase()
//        {
//            using var conn = new SQLiteConnection(_connectionString);
//            conn.Open();

//            const string createTableSql = @"CREATE TABLE IF NOT EXISTS FileInfo (
//            Path TEXT NOT NULL,
//            LastWriteTime TEXT NOT NULL,
//            Size INTEGER NOT NULL,
//            CheckSum TEXT,
//            Name TEXT,
//            LastAccessTime TEXT,
//            DirectoryName TEXT,
//            Extension TEXT,
//            InArchive INTEGER CHECK (InArchive IN (0,1)),
//            ArchiveCRC INTEGER,
//            ArchivePath TEXT,
//            ArchiveExtension TEXT,
//            ArchiveFileName TEXT,
//            Container_Path TEXT,
//            Container_LastWriteTime TEXT,
//            Container_Size INTEGER,
//            PRIMARY KEY (Path, LastWriteTime, Size),
//            FOREIGN KEY (Container_Path, Container_LastWriteTime, Container_Size) 
//                REFERENCES FileInfo(Path, LastWriteTime, Size) ON DELETE SET NULL
//        )";

//            new SQLiteCommand(createTableSql, conn).ExecuteNonQuery();
//        }

//        public IEnumerable<ExtendedFileInfo> Get(string path, DateTime lastWriteTime, ulong size)
//        {
//            var results = new List<ExtendedFileInfo>();

//            using var conn = new SQLiteConnection(_connectionString);
//            conn.Open();

//            const string sql = @"SELECT * FROM FileInfo 
//                            WHERE Path = @Path 
//                            AND LastWriteTime = @LastWriteTime 
//                            AND Size = @Size";
//            using var cmd = new SQLiteCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@Path", path);
//            cmd.Parameters.AddWithValue("@LastWriteTime", lastWriteTime.ToString("O"));
//            cmd.Parameters.AddWithValue("@Size", (long)size);

//            using var reader = cmd.ExecuteReader();
//            while (reader.Read())
//            {
//                results.Add(ReadExtendedFileInfo(reader));
//            }
//            return results;
//        }

//        public void Add(ExtendedFileInfo info)
//        {
//            Add(new[] { info });
//        }

//        public void Add(IEnumerable<ExtendedFileInfo> infos)
//        {
//            using var conn = new SQLiteConnection(_connectionString);
//            conn.Open();

//            const string sql = @"INSERT OR REPLACE INTO FileInfo (
//            Path, LastWriteTime, Size, CheckSum, Name, LastAccessTime, 
//            DirectoryName, Extension, InArchive, ArchiveCRC, ArchivePath,
//            ArchiveExtension, ArchiveFileName, Container_Path, 
//            Container_LastWriteTime, Container_Size
//        ) VALUES (
//            @Path, @LastWriteTime, @Size, @CheckSum, @Name, @LastAccessTime, 
//            @DirectoryName, @Extension, @InArchive, @ArchiveCRC, @ArchivePath,
//            @ArchiveExtension, @ArchiveFileName, @Container_Path, 
//            @Container_LastWriteTime, @Container_Size
//        )";

//            var transaction = conn.BeginTransaction();
//            try
//            {
//                foreach (var info in infos)
//                {
//                    using var cmd = new SQLiteCommand(sql, conn);
//                    AddParameters(cmd, info);
//                    cmd.ExecuteNonQuery();
//                }
//                transaction.Commit();
//            }
//            catch
//            {
//                transaction.Rollback();
//                throw;
//            }
//        }

//        private void AddParameters(SQLiteCommand cmd, ExtendedFileInfo info)
//        {
//            cmd.Parameters.AddWithValue("@Path", info.Path);
//            cmd.Parameters.AddWithValue("@LastWriteTime", info.LastWriteTime.ToString("O"));
//            cmd.Parameters.AddWithValue("@Size", (long)info.Size);
//            cmd.Parameters.AddWithValue("@CheckSum", info.CheckSum);
//            cmd.Parameters.AddWithValue("@Name", info.Name);
//            cmd.Parameters.AddWithValue("@LastAccessTime", info.LastAccessTime.ToString("O"));
//            cmd.Parameters.AddWithValue("@DirectoryName", info.DirectoryName);
//            cmd.Parameters.AddWithValue("@Extension", info.Extension);
//            cmd.Parameters.AddWithValue("@InArchive", info.InArchive ? 1 : 0);
//            cmd.Parameters.AddWithValue("@ArchiveCRC", (int)info.ArchiveCRC);
//            cmd.Parameters.AddWithValue("@ArchivePath", info.ArchivePath);
//            cmd.Parameters.AddWithValue("@ArchiveExtension", info.ArchiveExtension);
//            cmd.Parameters.AddWithValue("@ArchiveFileName", info.ArchiveFileName);

//            // Handle container reference
//            cmd.Parameters.AddWithValue("@Container_Path", info.Container?.Path);
//            cmd.Parameters.AddWithValue("@Container_LastWriteTime",
//                info.Container?.LastWriteTime.ToString("O"));
//            cmd.Parameters.AddWithValue("@Container_Size",
//                info.Container != null ? (long?)info.Container.Size : null);
//        }

//        private ExtendedFileInfo ReadExtendedFileInfo(SQLiteDataReader reader)
//        {
//            // Create container stub if foreign key exists
//            ExtendedFileInfo container = null;
//            if (!reader.IsDBNull(reader.GetOrdinal("Container_Path")))
//            {
//                container = new ExtendedFileInfo
//                {
//                    Path = reader["Container_Path"].ToString(),
//                    LastWriteTime = DateTime.Parse(reader["Container_LastWriteTime"].ToString()),
//                    Size = (ulong)Convert.ToInt64(reader["Container_Size"])
//                };
//            }

//            return new ExtendedFileInfo
//            {
//                Path = reader["Path"].ToString(),
//                LastWriteTime = DateTime.Parse(reader["LastWriteTime"].ToString()),
//                Size = (ulong)Convert.ToInt64(reader["Size"]),
//                CheckSum = reader["CheckSum"].ToString(),
//                Name = reader["Name"].ToString(),
//                LastAccessTime = DateTime.Parse(reader["LastAccessTime"].ToString()),
//                DirectoryName = reader["DirectoryName"] as string,
//                Extension = reader["Extension"].ToString(),
//                InArchive = Convert.ToInt32(reader["InArchive"]) == 1,
//                ArchiveCRC = (uint)Convert.ToInt32(reader["ArchiveCRC"]),
//                ArchivePath = reader["ArchivePath"].ToString(),
//                ArchiveExtension = reader["ArchiveExtension"].ToString(),
//                ArchiveFileName = reader["ArchiveFileName"].ToString(),
//                Container = container
//            };
//        }
//    }
//}
