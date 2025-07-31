//namespace DupTerminator.DataBase
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Data.SQLite;
//    using System.IO;
//    using System.Linq;
//    using DupTerminator.BusinessLogic;
//    using Microsoft.Data.Sqlite;
//    using Newtonsoft.Json;

//    public class SQLiteFileRepository : IDisposable
//    {
//        private readonly string _connectionString;
//        private SQLiteConnection _connection;

//        public SQLiteFileRepository(string dbPath)
//        {
//            _connectionString = $"Data Source={dbPath};Version=3;";
//            InitializeDatabase();
//        }

//        private void InitializeDatabase()
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();

//                // Create table if not exists
//                string createTableQuery = @"
//                CREATE TABLE IF NOT EXISTS ExtendedFileInfo (
//                    Path TEXT NOT NULL,
//                    LastWriteTime TEXT NOT NULL,
//                    Size INTEGER NOT NULL,
//                    CheckSum TEXT,
//                    Name TEXT NOT NULL,
//                    LastAccessTime TEXT NOT NULL,
//                    DirectoryName TEXT,
//                    Extension TEXT NOT NULL,
//                    InArchive INTEGER NOT NULL DEFAULT 0,
//                    ArchiveCRC INTEGER,
//                    ArchivePath TEXT,
//                    ArchiveExtension TEXT,
//                    ArchiveFileName TEXT,
//                    ContainerJson TEXT,
//                    PRIMARY KEY (Path, LastWriteTime, Size)
//                )";

//                using (var command = new SQLiteCommand(createTableQuery, connection))
//                {
//                    command.ExecuteNonQuery();
//                }
//            }
//        }

//        // Get a single ExtendedFileInfo by primary key
//        public ExtendedFileInfo Get(string path, DateTime lastWriteTime, ulong size)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();

//                string query = @"
//                SELECT Path, LastWriteTime, Size, CheckSum, Name, LastAccessTime,
//                       DirectoryName, Extension, InArchive, ArchiveCRC, ArchivePath,
//                       ArchiveExtension, ArchiveFileName, ContainerJson
//                FROM ExtendedFileInfo 
//                WHERE Path = @Path AND LastWriteTime = @LastWriteTime AND Size = @Size";

//                using (var command = new SQLiteCommand(query, connection))
//                {
//                    command.Parameters.AddWithValue("@Path", path);
//                    command.Parameters.AddWithValue("@LastWriteTime", lastWriteTime.ToString("O"));
//                    command.Parameters.AddWithValue("@Size", size);

//                    using (var reader = command.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            return MapToExtendedFileInfo(reader);
//                        }
//                        return null;
//                    }
//                }
//            }
//        }

//        // Add or update a single ExtendedFileInfo
//        public void Add(ExtendedFileInfo info)
//        {
//            Add(new[] { info });
//        }

//        // Add multiple ExtendedFileInfo objects
//        public void Add(IEnumerable<ExtendedFileInfo> infos)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();

//                // Use a transaction for bulk inserts
//                using (var transaction = connection.BeginTransaction())
//                {
//                    foreach (var info in infos)
//                    {
//                        AddInfo(connection, info);
//                    }

//                    transaction.Commit();
//                }
//            }
//        }

//        private void AddInfo(SQLiteConnection connection, ExtendedFileInfo info)
//        {
//            // Serialize Container if not null
//            string containerJson = null;
//            if (info.Container != null)
//            {
//                containerJson = JsonConvert.SerializeObject(info.Container);
//            }

//            // Use UPSERT (INSERT OR REPLACE) to handle updates
//            string upsertQuery = @"
//            INSERT OR REPLACE INTO ExtendedFileInfo (
//                Path, LastWriteTime, Size, CheckSum, Name, LastAccessTime, 
//                DirectoryName, Extension, InArchive, ArchiveCRC, ArchivePath,
//                ArchiveExtension, ArchiveFileName, ContainerJson
//            ) VALUES (
//                @Path, @LastWriteTime, @Size, @CheckSum, @Name, @LastAccessTime,
//                @DirectoryName, @Extension, @InArchive, @ArchiveCRC, @ArchivePath,
//                @ArchiveExtension, @ArchiveFileName, @ContainerJson
//            )";

//            using (var command = new SQLiteCommand(upsertQuery, connection))
//            {
//                command.Parameters.AddWithValue("@Path", info.Path ?? DBNull.Value);
//                command.Parameters.AddWithValue("@LastWriteTime", info.LastWriteTime.ToString("O"));
//                command.Parameters.AddWithValue("@Size", info.Size);
//                command.Parameters.AddWithValue("@CheckSum", info.CheckSum ?? DBNull.Value);
//                command.Parameters.AddWithValue("@Name", info.Name);
//                command.Parameters.AddWithValue("@LastAccessTime", info.LastAccessTime.ToString("O"));
//                command.Parameters.AddWithValue("@DirectoryName", info.DirectoryName ?? DBNull.Value);
//                command.Parameters.AddWithValue("@Extension", info.Extension);
//                command.Parameters.AddWithValue("@InArchive", info.InArchive ? 1 : 0);
//                command.Parameters.AddWithValue("@ArchiveCRC", info.ArchiveCRC == 0 ? (object)DBNull.Value : info.ArchiveCRC);
//                command.Parameters.AddWithValue("@ArchivePath", info.ArchivePath ?? DBNull.Value);
//                command.Parameters.AddWithValue("@ArchiveExtension", info.ArchiveExtension ?? DBNull.Value);
//                command.Parameters.AddWithValue("@ArchiveFileName", info.ArchiveFileName ?? DBNull.Value);
//                command.Parameters.AddWithValue("@ContainerJson", containerJson ?? DBNull.Value);

//                command.ExecuteNonQuery();
//            }
//        }

//        // Convert data reader to ExtendedFileInfo
//        private ExtendedFileInfo MapToExtendedInfo(SQLiteDataReader reader)
//        {
//            return new ExtendedFileInfo
//            {
//                Path = reader["Path"] as string,
//                LastWriteTime = DateTime.Parse((string)reader["LastWriteTime"]),
//                Size = Convert.ToUInt64(reader["Size"]),
//                CheckSum = reader["CheckSum"] as string,
//                Name = (string)reader["Name"],
//                LastAccessTime = DateTime.Parse((string)reader["LastAccessTime"]),
//                DirectoryName = reader["DirectoryName"] as string,
//                Extension = (string)reader["Extension"],
//                InArchive = Convert.ToBoolean(reader["InArchive"]),
//                ArchiveCRC = reader["ArchiveCRC"] as uint? ?? 0,
//                ArchivePath = reader["ArchivePath"] as string,
//                ArchiveExtension = reader["ArchiveExtension"] as string,
//                ArchiveFileName = reader["ArchiveFileName"] as string,
//                ContainerJson = reader["ContainerJson"] as string
//            };
//        }

//        // Deserialize Container property if needed
//        private ExtendedFileInfo DeserializeContainer(ExtendedFileInfo info)
//        {
//            if (!string.IsNullOrEmpty(info.ContainerJson))
//            {
//                try
//                {
//                    info.Container = JsonConvert.DeserializeObject<ExtendedFileInfo>(info.ContainerJson);
//                }
//                catch
//                {
//                    // Log or handle deserialization error
//                    info.Container = null;
//                }
//            }
//            return info;
//        }

//        public void Dispose()
//        {
//            // Clean up resources if needed
//        }
//    }
//}
