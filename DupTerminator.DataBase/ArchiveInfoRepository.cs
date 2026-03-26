//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.IO.Compression;
//using System.Text.Json;
//using DupTerminator.BusinessLogic.Model;
//using DupTerminator.DataBase.Dto;
//using Microsoft.Data.Sqlite;
//using static DupTerminator.DataBase.Dto.ArchiveInfoMapper;

//namespace DupTerminator.DataBase
//{
//    public class ArchiveInfoRepository : IArchiveInfoRepository
//    {
//        private readonly string _connectionString;
//        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
//        {
//            WriteIndented = false,
//            TypeInfoResolver = ArchiveJsonContext.Default
//        };
//        //private readonly ArchiveJsonContext _jsonContext = new(new JsonSerializerOptions { WriteIndented = false });

//        //private static readonly MessagePackSerializerOptions _messagePackOptions =
//        //      MessagePackSerializerOptions.Standard
//        //          .WithResolver(ContractlessStandardResolver.Instance)
//        //          .WithCompression(MessagePackCompression.Lz4BlockArray);


//        public ArchiveInfoRepository(string connectionString = "Data Source=archiveInfo.db;")
//        {
//            _connectionString = connectionString;
//            Initialize();
//        }

//        private void Initialize()
//        {
//            using var connection = new SqliteConnection(_connectionString);
//            connection.Open();

//            using var createTable = connection.CreateCommand();
//            createTable.CommandText = @"
//            PRAGMA foreign_keys = ON;

//            CREATE TABLE IF NOT EXISTS ExtendedFileInfoContainer (
//                Path TEXT NOT NULL,
//                LastWriteTime INTEGER NOT NULL,
//                Size TEXT NOT NULL,
//                SerializedFiles BLOB,
//                PRIMARY KEY (Path, LastWriteTime, Size)
//            );";
//            createTable.ExecuteNonQuery();
//        }



//        public static T DecompressJsonData<T>(byte[] compressedData, JsonSerializerOptions jsonOptions)
//        {
//            using (var inputStream = new MemoryStream(compressedData))
//            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
//            using (var outputStream = new MemoryStream())
//            {
//                // Copy the decompressed data to a new stream
//                gzipStream.CopyTo(outputStream);
//                outputStream.Position = 0; // Reset position for reading

//                // Deserialize directly from the stream
//                return JsonSerializer.Deserialize<T>(outputStream, jsonOptions);
//            }
//        }

//        public ArchiveFileInfo[] Get(string path, DateTime lastWriteTime, ulong size)
//        {
//            using var connection = new SqliteConnection(_connectionString);
//            connection.Open();

//            using var cmd = connection.CreateCommand();
//            cmd.CommandText = @"
//            SELECT SerializedFiles FROM ExtendedFileInfoContainer
//            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

//            cmd.Parameters.AddWithValue("$path", path);
//            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
//            cmd.Parameters.AddWithValue("$size", size.ToString());

//            using var reader = cmd.ExecuteReader();

//            if (reader.Read() && !reader.IsDBNull(0))
//            {
//                using var blobStream = reader.GetStream(0);

//                // 2️⃣ Decompress on‑the‑fly
//                using var gzipStream = new GZipStream(blobStream, CompressionMode.Decompress);

//                // 3️⃣ Deserialize straight from the decompressed stream
//                return JsonSerializer.Deserialize<ArchiveFileInfo[]>(gzipStream, _jsonOptions);
//                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(gzipStream, _jsonContext.ArchiveFileInfoArray);

//                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(data, _jsonOptions);
//                //return DecompressJsonData<ArchiveFileInfo[]>(data, _jsonOptions);
//            }

//            return null;
//        }

//        public bool Exist(string path, DateTime lastWriteTime, ulong size)
//        {
//            using var connection = new SqliteConnection(_connectionString);
//            connection.Open();

//            using var cmd = connection.CreateCommand();
//            // SELECT EXISTS is the standard, high-performance way to check for records in SQLite
//            cmd.CommandText = @"
//                SELECT EXISTS(
//                    SELECT 1 FROM ExtendedFileInfoContainer 
//                    WHERE Path = $path 
//                      AND LastWriteTime = $lastWriteTime 
//                      AND Size = $size
//                );";

//            cmd.Parameters.AddWithValue("$path", path);
//            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
//            cmd.Parameters.AddWithValue("$size", size.ToString());

//            // ExecuteScalar returns the first column of the first row (1 for true, 0 for false)
//            var result = cmd.ExecuteScalar();
//            return Convert.ToInt32(result) == 1;
//        }


//        public void Add(ExtendedFileInfo container, IEnumerable<ArchiveFileInfo> files)
//        {
//            Debug.Assert(container is not null);
//            using var connection = new SqliteConnection(_connectionString);
//            connection.Open();

//            //var jsonData = JsonSerializer.Serialize(files, _jsonOptions);
//            var jsonData = JsonHelper.CompressJsonData(files, _jsonOptions);
//            //var jsonStream = CompressJsonData(files, _jsonOptions);


//            using var cmd = connection.CreateCommand();
//            cmd.CommandText = @"
//            INSERT INTO ExtendedFileInfoContainer(Path, LastWriteTime, Size, SerializedFiles)
//            VALUES ($path, $lastWriteTime, $size, $serialized)
//            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
//                SerializedFiles = $serialized;";

//            cmd.Parameters.AddWithValue("$path", container.Path);
//            cmd.Parameters.AddWithValue("$lastWriteTime", container.LastWriteTime.Ticks);
//            cmd.Parameters.AddWithValue("$size", container.Size.ToString());
//            cmd.Parameters.AddWithValue("$serialized", jsonData);
//            // Use SqliteParameter with stream
//            //var param = cmd.Parameters.Add("$serialized", SqliteType.Blob);
//            //param.Value = jsonStream;

//            cmd.ExecuteNonQuery();
//        }

//        /// <summary>
//        /// Enumerates **all** records stored in the SQLite database.
//        /// Each yielded element is the deserialized <c>ArchiveFileInfo[]</c> for a row.
//        /// </summary>
//        public IEnumerable<string> EnumerateAllPath()
//        {
//            using var connection = new SqliteConnection(_connectionString);
//            connection.Open();

//            // NOTE: we only need the BLOB column – the other columns are useful for debugging
//            using var cmd = connection.CreateCommand();
//            cmd.CommandText = @"
//            SELECT Path
//            FROM ExtendedFileInfoContainer;";

//            using var reader = cmd.ExecuteReader();

//            while (reader.Read())
//            {
//                // Yield the result – the caller can iterate lazily
//                yield return reader.GetString(0);
//            }
//        }

//        /// <summary>
//        /// When data is deleted from an SQLite database, the space is marked as available for reuse, but the physical file size on disk usually doesn't shrink immediately.
//        /// To reclaim this unused space and reduce the file size, you must run the VACUUM command. 
//        /// </summary>
//        /// <param name="connectionString"></param>
//        public void VacuumDatabase()
//        {
//            using (var connection = new SqliteConnection(_connectionString))
//            {
//                connection.Open();

//                var command = connection.CreateCommand();
//                command.CommandText = "VACUUM";
//                command.ExecuteNonQuery();

//                Debug.WriteLine("Database file size optimized with VACUUM.");
//            }
//        }
//    }
//}
