using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;
using Microsoft.Data.Sqlite;

namespace DupTerminator.DataBase
{
    public class PhashRepository : IPhashRepository
    {
        private readonly string _connectionString;
        private readonly object _lock = new();

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
        };

        public PhashRepository(string connectionString = "Data Source=phash.db;")
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

            CREATE TABLE IF NOT EXISTS PHashTable (
                Path TEXT NOT NULL,
                LastWriteTime INTEGER NOT NULL,
                Size TEXT NOT NULL,
                Phash TEXT NOT NULL,
                Width INTEGER,
                Height INTEGER,
                PRIMARY KEY (Path, LastWriteTime, Size)
            );

            CREATE TABLE IF NOT EXISTS PHashContainerTable (
                Path TEXT NOT NULL,
                LastWriteTime INTEGER NOT NULL,
                Size TEXT NOT NULL,
                SerializedFiles BLOB,
                PRIMARY KEY (Path, LastWriteTime, Size)
            );";
            createTable.ExecuteNonQuery();
        }

        public (ulong phash, int width, int height)? Get(string path, DateTime lastWriteTime, ulong size)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT Phash, Width, Height FROM PHashTable
            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());

            using SqliteDataReader? reader = cmd.ExecuteReader();

            if (reader.Read() && !reader.IsDBNull(0))
            {
                ulong phash = ulong.Parse(reader["Phash"].ToString());

                // Get the ordinal of the "MyIntColumn"
                int myIntColumnOrdinal = reader.GetOrdinal("Width");

                int width = 0;
                if (!reader.IsDBNull(myIntColumnOrdinal))
                {
                    width = reader.GetInt32(myIntColumnOrdinal);
                }
                myIntColumnOrdinal = reader.GetOrdinal("Height");

                int height = 0;
                if (!reader.IsDBNull(myIntColumnOrdinal))
                {
                    height = reader.GetInt32(myIntColumnOrdinal);
                }
                return (phash, width, height);
            }

            return null;
        }

        public void Add(string path, DateTime lastWriteTime, ulong size, ulong phash, int width, int height)
        {
            if (lastWriteTime == DateTime.MinValue)
                throw new ArgumentOutOfRangeException(nameof(lastWriteTime));

            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO PHashTable(Path, LastWriteTime, Size, Phash, Width, Height)
            VALUES ($path, $lastWriteTime, $size, $phash, $width, $height)
            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
                Phash = $phash,
                Width = $width,
                Height = $height;";

                cmd.Parameters.AddWithValue("$path", path);
                cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
                cmd.Parameters.AddWithValue("$size", size.ToString());
                cmd.Parameters.AddWithValue("$phash", phash.ToString());
                cmd.Parameters.AddWithValue("$width", width);
                cmd.Parameters.AddWithValue("$height", height);

                cmd.ExecuteNonQuery();
            }
        }

        public (ExtendedFileInfo efi, ulong phash, int width, int height)[] GetContainerHashes(ExtendedFileInfo fileInfo)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT SerializedFiles FROM PHashContainerTable
            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

            cmd.Parameters.AddWithValue("$path", fileInfo.Path);
            cmd.Parameters.AddWithValue("$lastWriteTime", fileInfo.LastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", fileInfo.Size.ToString());

            using var reader = cmd.ExecuteReader();

            if (reader.Read() && !reader.IsDBNull(0))
            {
                //var data = (string)reader.GetValue(0);
                //return JsonSerializer.Deserialize<(ExtendedFileInfo efi, ulong phash, int width, int height)[]>(data, _jsonOptions);

                using var blobStream = reader.GetStream(0);

                // 2️⃣ Decompress on‑the‑fly
                using var gzipStream = new GZipStream(blobStream, CompressionMode.Decompress);

                // 3️⃣ Deserialize straight from the decompressed stream
                return JsonSerializer.Deserialize<(ExtendedFileInfo efi, ulong phash, int width, int height)[]>(gzipStream, _jsonOptions);
            }

            return null;
        }

        public void AddContainerStreams(ExtendedFileInfo fileInfo, (ExtendedFileInfo efi, ulong phash, int width, int height)[] collection)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            //var jsonData = JsonSerializer.Serialize(collection, _jsonOptions);
            var jsonData = JsonHelper.CompressJsonData(collection, _jsonOptions);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO PHashContainerTable(Path, LastWriteTime, Size, SerializedFiles)
            VALUES ($path, $lastWriteTime, $size, $serialized)
            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
                SerializedFiles = $serialized;";

            cmd.Parameters.AddWithValue("$path", fileInfo.Path);
            cmd.Parameters.AddWithValue("$lastWriteTime", fileInfo.LastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", fileInfo.Size.ToString());
            cmd.Parameters.AddWithValue("$serialized", jsonData);

            cmd.ExecuteNonQuery();
        }

        public string[] GetAllContainerPath()
        {
            var paths = new List<string>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Path FROM PHashContainerTable;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                paths.Add(reader.GetString(0));
            }

            return paths.ToArray();
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
                DELETE FROM PHashContainerTable
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
                return affectedRows;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException(
                    $"Error deleting records for path {path}", ex);
            }
        }
    }
}
