using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using DupTerminator.BusinessLogic.Model;
using Microsoft.Data.Sqlite;

namespace DupTerminator.DataBase
{
    public class PdfInfoRepository : IPdfInfoRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        public PdfInfoRepository(string connectionString = "Data Source=pdfInfo.db;")
        {
            _connectionString = connectionString;
            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var createTable = connection.CreateCommand();
            createTable.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS PdfFileInfoTable (
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

        public PdfFileInfo[] Get(string path, DateTime lastWriteTime, ulong size)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT SerializedFiles FROM PdfFileInfoTable
            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());

            using var reader = cmd.ExecuteReader();

            if (reader.Read() && !reader.IsDBNull(0))
            {
                using var blobStream = reader.GetStream(0);

                // 2️⃣ Decompress on‑the‑fly
                using var gzipStream = new GZipStream(blobStream, CompressionMode.Decompress);

                // 3️⃣ Deserialize straight from the decompressed stream
                return JsonSerializer.Deserialize<PdfFileInfo[]>(gzipStream, _jsonOptions);

                //return JsonSerializer.Deserialize<ArchiveFileInfo[]>(data, _jsonOptions);
                //return DecompressJsonData<ArchiveFileInfo[]>(data, _jsonOptions);
            }

            return null;
        }
        private static byte[] CompressJsonData<T>(T data, JsonSerializerOptions options)
        {
            // 1. Serialize the object to a UTF-8 byte array
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);

            // 2. Compress the byte array using GZipStream
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }
                // The compressed data is now in the outputStream
                return outputStream.ToArray();
            }
        }

        public void Add(ExtendedFileInfo container, IEnumerable<PdfFileInfo> files)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            //var jsonData = JsonSerializer.Serialize(files, _jsonOptions);
            var jsonData = CompressJsonData(files, _jsonOptions);


            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO PdfFileInfoTable(Path, LastWriteTime, Size, SerializedFiles)
            VALUES ($path, $lastWriteTime, $size, $serialized)
            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
                SerializedFiles = $serialized;";

            cmd.Parameters.AddWithValue("$path", container.Path);
            cmd.Parameters.AddWithValue("$lastWriteTime", container.LastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", container.Size.ToString());
            cmd.Parameters.AddWithValue("$serialized", jsonData);

            cmd.ExecuteNonQuery();
        }
    }
}
