using Microsoft.Data.Sqlite;
using Glide.Core.Models;
using System.IO;

namespace Glide.App.Services;

public class ConnectDbService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public ConnectDbService(string? customDbPath = null)
    {
        if (customDbPath != null)
        {
            _dbPath = customDbPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string glideDir = Path.Combine(appData, "Glide");
            Directory.CreateDirectory(glideDir);
            _dbPath = Path.Combine(glideDir, "connect.db");
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Rooms (
                RoomCode TEXT PRIMARY KEY,
                CreatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id TEXT PRIMARY KEY,
                RoomCode TEXT NOT NULL,
                SenderName TEXT NOT NULL,
                Content TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                ImagePath TEXT,
                IsSelf INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS IX_Messages_RoomCode ON Messages(RoomCode);
        ";
        cmd.ExecuteNonQuery();
    }

    public void EnsureRoom(string roomCode)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Rooms (RoomCode, CreatedAt, IsActive)
            VALUES (@code, @createdAt, 1);
        ";
        cmd.Parameters.AddWithValue("@code", roomCode);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void SaveMessage(string roomCode, ConnectChatMessage message)
    {
        EnsureRoom(roomCode);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Messages (Id, RoomCode, SenderName, Content, Timestamp, ImagePath, IsSelf)
            VALUES (@id, @roomCode, @sender, @content, @timestamp, @imagePath, @isSelf);
        ";
        cmd.Parameters.AddWithValue("@id", message.Id);
        cmd.Parameters.AddWithValue("@roomCode", roomCode);
        cmd.Parameters.AddWithValue("@sender", message.SenderName);
        cmd.Parameters.AddWithValue("@content", message.Content);
        cmd.Parameters.AddWithValue("@timestamp", message.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("@imagePath", (object?)message.ImagePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@isSelf", message.IsSelf ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public List<ConnectChatMessage> GetMessages(string roomCode)
    {
        var list = new List<ConnectChatMessage>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, SenderName, Content, Timestamp, ImagePath, IsSelf
            FROM Messages
            WHERE RoomCode = @roomCode
            ORDER BY Timestamp ASC;
        ";
        cmd.Parameters.AddWithValue("@roomCode", roomCode);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string id = reader.GetString(0);
            string sender = reader.GetString(1);
            string content = reader.GetString(2);
            DateTime ts = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.Now;
            string? imagePath = reader.IsDBNull(4) ? null : reader.GetString(4);
            bool isSelf = reader.GetInt32(5) == 1;

            list.Add(new ConnectChatMessage(
                Id: id,
                SenderName: sender,
                Content: content,
                Timestamp: ts,
                ImagePath: imagePath,
                IsSelf: isSelf
            ));
        }

        return list;
    }

    public void ClearMessages(string roomCode)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Messages WHERE RoomCode = @roomCode;";
        cmd.Parameters.AddWithValue("@roomCode", roomCode);
        cmd.ExecuteNonQuery();
    }
}

