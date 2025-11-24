using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

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
                PRIMARY KEY (Path, LastWriteTime, Size)
            );";
            createTable.ExecuteNonQuery();
        }

        public ulong? Get(string path, DateTime lastWriteTime, ulong size)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            SELECT Phash FROM PHashTable
            WHERE Path = $path AND LastWriteTime = $lastWriteTime AND Size = $size;";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());

            using var reader = cmd.ExecuteReader();

            if (reader.Read() && !reader.IsDBNull(0))
            {
                var data = reader.GetValue(0);
                return ulong.Parse(data.ToString());
            }

            return null;
        }

        public void Add(string path, DateTime lastWriteTime, ulong size, ulong phash)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO PHashTable(Path, LastWriteTime, Size, Phash)
            VALUES ($path, $lastWriteTime, $size, $phash)
            ON CONFLICT (Path, LastWriteTime, Size) DO UPDATE SET
                Phash = $phash;";

            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$lastWriteTime", lastWriteTime.Ticks);
            cmd.Parameters.AddWithValue("$size", size.ToString());
            cmd.Parameters.AddWithValue("$phash", phash.ToString());

            cmd.ExecuteNonQuery();
        }
    }
}
