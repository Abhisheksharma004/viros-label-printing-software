using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using LabelPrinterApp.Models;

namespace LabelPrinterApp.Services
{
    public static class Database
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
        private static readonly string ConnectionString = $"Data Source={DbPath.Replace("\\\\", "/")}";

        public static void Initialize()
        {
            var firstTime = !File.Exists(DbPath);
            if (firstTime) using (File.Create(DbPath)) { }

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    password TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS designs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    prn_content TEXT NOT NULL,
                    width_inches REAL DEFAULT 4,
                    height_inches REAL DEFAULT 6,
                    dpmm INTEGER DEFAULT 8,
                    preview_path TEXT,
                    created_at TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS print_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    design_id INTEGER NOT NULL,
                    serial_number INTEGER NOT NULL,
                    printed_at TEXT NOT NULL,
                    is_reprint INTEGER DEFAULT 0,
                    FOREIGN KEY (design_id) REFERENCES designs(id)
                );
            ";
            cmd.ExecuteNonQuery();

            // Update existing databases to add is_reprint column if it doesn't exist
            try
            {
                var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "ALTER TABLE print_logs ADD COLUMN is_reprint INTEGER DEFAULT 0;";
                updateCmd.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists, ignore the error
            }

            if (firstTime)
            {
                var seed = conn.CreateCommand();
                seed.CommandText = "INSERT INTO users (username, password) VALUES ('admin','admin');";
                seed.ExecuteNonQuery();

                var sample = conn.CreateCommand();
                sample.CommandText = "INSERT INTO designs (name, prn_content, width_inches, height_inches, dpmm, created_at) VALUES (@n,@p,@w,@h,@d,@c);";
                sample.Parameters.AddWithValue("@n", "Sample ZPL with All Shortcut Codes");
                sample.Parameters.AddWithValue("@p", "^XA\n^FO50,50^A0N,35,35^FDSerial: {SERIAL}^FS\n^FO50,100^A0N,30,30^FD2-digit: {SERIAL1}^FS\n^FO50,150^A0N,30,30^FD3-digit: {SERIAL2}^FS\n^FO50,200^A0N,30,30^FD4-digit: {SERIAL3}^FS\n^FO50,250^A0N,25,25^FDDate: {DATE}^FS\n^FO50,300^A0N,25,25^FDTime: {TIME} Month: {CHAR_MM}^FS\n^FO50,350^A0N,25,25^FDCustom: {CUSTOM_TEXT}^FS\n^XZ");
                sample.Parameters.AddWithValue("@w", 4.0);
                sample.Parameters.AddWithValue("@h", 6.0);
                sample.Parameters.AddWithValue("@d", 8);
                sample.Parameters.AddWithValue("@c", DateTime.UtcNow.ToString("o"));
                sample.ExecuteNonQuery();
            }
        }

        public static bool ValidateUser(string username, string password)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM users WHERE username=@u AND password=@p;";
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        public static List<Design> GetDesigns()
        {
            var list = new List<Design>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,name,prn_content,width_inches,height_inches,dpmm,preview_path,created_at FROM designs ORDER BY datetime(created_at) DESC;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Design
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    PrnContent = r.GetString(2),
                    WidthInches = r.GetDouble(3),
                    HeightInches = r.GetDouble(4),
                    Dpmm = r.GetInt32(5),
                    PreviewPath = r.IsDBNull(6) ? "" : r.GetString(6),
                    CreatedAt = DateTime.Parse(r.GetString(7))
                });
            }
            return list;
        }

        public static int SaveDesign(string name, string prn, double widthInches, double heightInches, int dpmm, int id = 0, string? previewPath = null)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (id == 0)
            {
                cmd.CommandText = "INSERT INTO designs (name, prn_content, width_inches, height_inches, dpmm, preview_path, created_at) VALUES (@n,@p,@w,@h,@d,@pp,@c);";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", prn);
                cmd.Parameters.AddWithValue("@w", widthInches);
                cmd.Parameters.AddWithValue("@h", heightInches);
                cmd.Parameters.AddWithValue("@d", dpmm);
                cmd.Parameters.AddWithValue("@pp", (object?)previewPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@c", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = "UPDATE designs SET name=@n, prn_content=@p, width_inches=@w, height_inches=@h, dpmm=@d, preview_path=@pp WHERE id=@id;";
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@p", prn);
                cmd.Parameters.AddWithValue("@w", widthInches);
                cmd.Parameters.AddWithValue("@h", heightInches);
                cmd.Parameters.AddWithValue("@d", dpmm);
                cmd.Parameters.AddWithValue("@pp", (object?)previewPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return id;
            }
        }

        public static void SaveDesignPreviewPath(int designId, string path)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE designs SET preview_path=@p WHERE id=@id;";
            cmd.Parameters.AddWithValue("@p", path);
            cmd.Parameters.AddWithValue("@id", designId);
            cmd.ExecuteNonQuery();
        }

        public static string GetDesignPreviewPath(int designId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT preview_path FROM designs WHERE id=@id;";
            cmd.Parameters.AddWithValue("@id", designId);
            var res = cmd.ExecuteScalar();
            return res?.ToString() ?? "";
        }

        public static string GetPrnByDesignId(int designId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT prn_content FROM designs WHERE id=@id;";
            cmd.Parameters.AddWithValue("@id", designId);
            var res = cmd.ExecuteScalar();
            return res?.ToString() ?? "";
        }

        public static (double w, double h, int d) GetDesignSize(int designId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT width_inches, height_inches, dpmm FROM designs WHERE id=@id;";
            cmd.Parameters.AddWithValue("@id", designId);
            using var r = cmd.ExecuteReader();
            if (r.Read()) return (r.GetDouble(0), r.GetDouble(1), r.GetInt32(2));
            return (4.0, 6.0, 8);
        }

        public static int GetLastPrintedSerial(int designId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT MAX(serial_number) FROM print_logs WHERE design_id=@id;";
            cmd.Parameters.AddWithValue("@id", designId);
            var res = cmd.ExecuteScalar();
            if (res == DBNull.Value || res == null) return 0;
            return Convert.ToInt32(res);
        }

        public static void LogPrintedSerial(int designId, int serial, bool isReprint = false)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO print_logs (design_id, serial_number, printed_at, is_reprint) VALUES (@d,@s,@t,@r);";
            cmd.Parameters.AddWithValue("@d", designId);
            cmd.Parameters.AddWithValue("@s", serial);
            cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@r", isReprint ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public static DataTable GetPrintLogs(string? designFilter = null, int? serialFilter = null)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var sql = @"SELECT l.id as LogId, d.name as DesignName, l.serial_number as Serial, l.printed_at as PrintedAt, d.id as DesignId, 
                        CASE WHEN l.is_reprint = 1 THEN 'Reprint' ELSE 'Original' END as PrintType
                        FROM print_logs l
                        JOIN designs d ON l.design_id = d.id
                        WHERE 1=1 ";
            using var cmd = conn.CreateCommand();
            if (!string.IsNullOrWhiteSpace(designFilter))
            {
                sql += " AND d.name LIKE @df ";
                cmd.Parameters.AddWithValue("@df", $"%{designFilter}%");
            }
            if (serialFilter.HasValue)
            {
                sql += " AND l.serial_number = @sf ";
                cmd.Parameters.AddWithValue("@sf", serialFilter.Value);
            }
            sql += " ORDER BY datetime(l.printed_at) DESC;";
            cmd.CommandText = sql;
            var dt = new DataTable();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }
    }
}
