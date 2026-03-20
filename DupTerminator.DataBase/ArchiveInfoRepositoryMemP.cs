using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase.Dto;
using MemoryPack;
using MemoryPack.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DupTerminator.DataBase
{
    public class ArchiveInfoRepositoryMemP : IArchiveInfoRepository
    {
        private readonly string _connectionString;

        public ArchiveInfoRepositoryMemP(string connectionString = "Data Source=archiveInfoMemP.db;")
        {
            _connectionString = connectionString;
            Initialize();

            //MemoryPackFormatterProvider.Register<ArchiveFileInfo[]>(new ArchiveFileInfoMPFormatter());
            MemoryPackFormatterProvider.Register<ArchiveFileInfo>(new ArchiveFileInfoMPFormatter2());
            //MemoryPackFormatterProvider.Register<ArchiveSimpleFileInfo>(new ArchiveSimpleFileInfoFormatter());
            //MemoryPackFormatterProvider.Register<SimpleFileInfo>(new SimpleFileInfoFormatter());
            //MemoryPackFormatterProvider.Register<ExtendedFileInfo>(new ExtendedFileInfoFormatter());
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var createTable = connection.CreateCommand();
            createTable.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS ExtendedFileInfoContainer (
                Path TEXT NOT NULL,
                LastWriteTime INTEGER NOT NULL,
                Size TEXT NOT NULL,
                SerializedFiles BLOB,
                PRIMARY KEY (Path, LastWriteTime, Size)
            );";
            createTable.ExecuteNonQuery();
        }



        public static T DecompressJsonData<T>(byte[] compressedData, JsonSerializerOptions jsonOptions)
        {
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                // Copy the decompressed data to a new stream
                gzipStream.CopyTo(outputStream);
                outputStream.Position = 0; // Reset position for reading

                // Deserialize directly from the stream
                return JsonSerializer.Deserialize<T>(outputStream, jsonOptions);
            }
        }

        public ArchiveFileInfo[] Get(string path, DateTime lastWriteTime, ulong size)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT SerializedFiles FROM ExtendedFileInfoContainer
            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());

            using var reader = cmd.ExecuteReader();

            if (reader.Read() && !reader.IsDBNull(0))
            {
                //using var blobStream = reader.GetStream(0);

                // 2️⃣ Decompress on‑the‑fly
                //using var gzipStream = new GZipStream(blobStream, CompressionMode.Decompress);

                // 3️⃣ Deserialize straight from the decompressed stream
                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(gzipStream, _jsonOptions);
                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(gzipStream, _jsonContext.ArchiveFileInfoArray);

                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(data, _jsonOptions);
                //return DecompressJsonData<ArchiveFileInfo[]>(data, _jsonOptions);

                // No GZip needed - MessagePack has built-in LZ4 compression!
                //var data = (byte[])reader.GetValue(0);
                //return MessagePackSerializer.Deserialize<ArchiveFileInfo[]>(data, _messagePackOptions);

                // Get compressed data as byte array
                var compressedData = (byte[])reader.GetValue(0);

                // Decompress with Brotli (MemoryPack's efficient compressor)
                using var decompressor = new BrotliDecompressor();
                var decompressedBuffer = decompressor.Decompress(compressedData);

                // Deserialize DTOs
                //var dtos = MemoryPackSerializer.Deserialize<ArchiveFileInfoDto[]>(decompressedBuffer);

                // Map DTOs to entities
                //return dtos?.ToEntityArray();
                return MemoryPackSerializer.Deserialize<ArchiveFileInfo[]>(decompressedBuffer);
            }

            return null;
        }
        public bool Exist(string path, DateTime lastWriteTime, ulong size)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            // SELECT EXISTS is the standard, high-performance way to check for records in SQLite
            cmd.CommandText = @"
                SELECT EXISTS(
                    SELECT 1 FROM ExtendedFileInfoContainer 
                    WHERE Path = $path 
                      AND LastWriteTime = $lastWriteTime 
                      AND Size = $size
                );";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());

            // ExecuteScalar returns the first column of the first row (1 for true, 0 for false)
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result) == 1;
        }

        //private static Stream CompressJsonDataToStream<T>(T data, JsonSerializerOptions options)
        //{
        //    var outputStream = new MemoryStream();
        //    var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal);

        //    // Create a UTF8JsonWriter that writes directly to the GZipStream
        //    using var jsonWriter = new Utf8JsonWriter(gzipStream);

        //    JsonSerializer.Serialize(jsonWriter, data, options);
        //    //jsonWriter.WriteStartArray();
        //    //foreach (var item in data)
        //    //{
        //    //JsonSerializer.Serialize(jsonWriter, item, options);
        //    //}
        //    //jsonWriter.WriteEndArray();

        //    jsonWriter.Flush();
        //    //gzipStream.Close(); // This also flushes the gzip stream

        //    outputStream.Position = 0; // Reset position for reading
        //    return outputStream;
        //}

        //private static Stream CompressJsonData<T>(T data, JsonSerializerOptions options)
        //{
        //    // 1. Serialize the object to a UTF-8 byte array
        //    byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);

        //    // 2. Compress the byte array using GZipStream
        //    var outputStream = new MemoryStream();
        //    using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        //    {
        //        gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
        //    }
        //    // The compressed data is now in the outputStream
        //    return outputStream;
        //}

        private static byte[] CompressJsonData<T>(T data, JsonSerializerOptions options)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                // Serialize directly into the GZip stream — no intermediate byte[]
                JsonSerializer.Serialize(gzipStream, data, options);
            }
            return outputStream.ToArray();
        }


        //private static byte[] CompressJsonData<T>(T data, JsonSerializerOptions options)
        //{
        //    // 1. Serialize the object to a UTF-8 byte array
        //    byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);

        //    // 2. Compress the byte array using GZipStream
        //    using (var outputStream = new MemoryStream())
        //    {
        //        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        //        {
        //            gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
        //        }
        //        // The compressed data is now in the outputStream
        //        return outputStream.ToArray();
        //    }
        //}

        public void Add(ExtendedFileInfo container, IEnumerable<ArchiveFileInfo> files)
        {
            Debug.Assert(container is not null);
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            //var jsonData = JsonSerializer.Serialize(files, _jsonOptions);
            //var jsonData = CompressJsonData(files, _jsonOptions);
            //var jsonStream = CompressJsonData(files, _jsonOptions);
            // Serialize with built-in compression
            //var serializedData = MessagePackSerializer.Serialize(files, _messagePackOptions);

            //Convert entities to DTOs
            //var dtos = files.ToDtoArray();
            // Compress with Brotli
            using var compressor = new BrotliCompressor();
            MemoryPackSerializer.Serialize(compressor, files);
            var serializedData = compressor.ToArray();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO ExtendedFileInfoContainer(Path, LastWriteTime, Size, SerializedFiles)
            VALUES ($path, $lastWriteTime, $size, $serialized)
            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
                SerializedFiles = $serialized;";

            cmd.Parameters.AddWithValue("$path", container.Path);
            cmd.Parameters.AddWithValue("$lastWriteTime", container.LastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", container.Size.ToString());
            cmd.Parameters.AddWithValue("$serialized", serializedData);
            // Use SqliteParameter with stream
            //var param = cmd.Parameters.Add("$serialized", SqliteType.Blob);
            //param.Value = jsonStream;

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Enumerates **all** records stored in the SQLite database.
        /// Each yielded element is the deserialized <c>ArchiveFileInfo[]</c> for a row.
        /// </summary>
        public IEnumerable<string> EnumerateAllPath()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // NOTE: we only need the BLOB column – the other columns are useful for debugging
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT Path
            FROM ExtendedFileInfoContainer;";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                // Yield the result – the caller can iterate lazily
                yield return reader.GetString(0);
            }
        }

        public int DeleteByPath(string path)
        {
            // Валидация входного параметра
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                DELETE FROM ExtendedFileInfoContainer
                WHERE Path = @path";

            // Добавляем параметр с правильным типом данных
            command.Parameters.Add(new SqliteParameter("@path", SqliteType.Text)
            {
                Value = path
            });

            command.Transaction = transaction;

            try
            {
                int affectedRows = command.ExecuteNonQuery();
                transaction.Commit();

                // Для отладки можно добавить логирование
                 //_logger.LogDebug($"Deleted {affectedRows} records for path: {path}");
                return affectedRows;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException(
                    $"Error deleting records for path {path}", ex);
            }
        }

        /// <summary>
        /// When data is deleted from an SQLite database, the space is marked as available for reuse, but the physical file size on disk usually doesn't shrink immediately.
        /// To reclaim this unused space and reduce the file size, you must run the VACUUM command. 
        /// </summary>
        /// <param name="connectionString"></param>
        public void VacuumDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "VACUUM";
                command.ExecuteNonQuery();

                Debug.WriteLine("Database file size optimized with VACUUM.");
            }
        }
    }
}
