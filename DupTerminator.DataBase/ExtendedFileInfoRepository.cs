using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DupTerminator.BusinessLogic.Model;
using Microsoft.Data.Sqlite;

namespace DupTerminator.DataBase
{
    public class ExtendedFileInfoRepository : IExtendedFileInfoRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        public ExtendedFileInfoRepository(string connectionString = "Data Source=archiveInfo.db;")
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

            CREATE TABLE IF NOT EXISTS ExtendedFileInfoContainer (
                Path TEXT NOT NULL,
                LastWriteTime INTEGER NOT NULL,
                Size TEXT NOT NULL,
                SerializedFiles BLOB,
                PRIMARY KEY (Path, LastWriteTime, Size)
            );";
            createTable.ExecuteNonQuery();
        }

        public IEnumerable<ArchiveFileInfo>? Get(string path, DateTime lastWriteTime, ulong size)
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
                var data = (string)reader.GetValue(0);
                return JsonSerializer.Deserialize<ArchiveFileInfo[]>(data, _jsonOptions);
            }

            return null;
        }

        public void Add(ExtendedFileInfo container, IEnumerable<ArchiveFileInfo> files)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var jsonData = JsonSerializer.Serialize(files, _jsonOptions);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO ExtendedFileInfoContainer(Path, LastWriteTime, Size, SerializedFiles)
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
