using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace AsistenciaDesktop_v2.Data
{
    public static class LocalDatabaseManager
    {
        private static readonly string dbPath = "asistencia_v2.db";
        private static readonly string connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Default Timeout=5";

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(connectionString);
        }

        public static void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmdWal = conn.CreateCommand();
                cmdWal.CommandText = "PRAGMA journal_mode=WAL;";
                cmdWal.ExecuteNonQuery();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Alumnos (
                        NumeroUnico TEXT PRIMARY KEY,
                        Nombre TEXT NOT NULL,
                        Curso TEXT NOT NULL,
                        EmailApoderado TEXT,
                        IsSynced INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS AsistenciaColegio (
                        AlumnoId TEXT,
                        Fecha TEXT,
                        HoraIngreso TEXT NOT NULL,
                        HoraSalida TEXT,
                        IsSynced INTEGER DEFAULT 0,
                        PRIMARY KEY (AlumnoId, Fecha)
                    );

                    CREATE TABLE IF NOT EXISTS AsistenciaComedor (
                        AlumnoId TEXT,
                        Fecha TEXT,
                        HoraIngreso TEXT NOT NULL,
                        IsSynced INTEGER DEFAULT 0,
                        PRIMARY KEY (AlumnoId, Fecha)
                    );

                                        CREATE TABLE IF NOT EXISTS Usuarios (
                        Username TEXT PRIMARY KEY,
                        PasswordHash TEXT NOT NULL,
                        Role TEXT NOT NULL,
                        Email TEXT,
                        IsSynced INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS EmailsQueue (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ToEmail TEXT NOT NULL,
                        Subject TEXT NOT NULL,
                        Body TEXT NOT NULL,
                        PdfBase64 TEXT,
                        PdfName TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Configuracion (
                        Clave TEXT PRIMARY KEY,
                        Valor TEXT
                    );
                    CREATE TABLE IF NOT EXISTS ReportesGenerados (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Fecha TEXT,
                        Tipo TEXT,
                        RutaArchivo TEXT,
                        Visto INTEGER DEFAULT 0
                    );
                ";
                cmd.ExecuteNonQuery();

                SeedUsers(conn);
            }
        }

        private static void SeedUsers(SqliteConnection conn)
        {
            var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE Username = 'cirdam'";
            long count = (long)cmdCheck.ExecuteScalar();

            if (count == 0)
            {
                var cmdInsert = conn.CreateCommand();
                cmdInsert.CommandText = "INSERT INTO Usuarios (Username, PasswordHash, Role, Email) VALUES (@user, @pass, @role, @email)";
                cmdInsert.Parameters.AddWithValue("@user", "cirdam");
                cmdInsert.Parameters.AddWithValue("@pass", HashPassword("1234567"));
                cmdInsert.Parameters.AddWithValue("@role", "admin");
                cmdInsert.Parameters.AddWithValue("@email", "notificaciones@liceotpggm.cl");
                cmdInsert.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password)
        {
            // PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 100000 iterations
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(256 / 8);
                byte[] hashBytes = new byte[salt.Length + hash.Length];
                Array.Copy(salt, 0, hashBytes, 0, salt.Length);
                Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            // Compatibilidad con el hash antiguo SHA256 (64 caracteres hex)
            if (storedHash.Length == 64 && Regex.IsMatch(storedHash, @"^[0-9a-f]+$"))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                    StringBuilder builder = new StringBuilder();
                    foreach (byte b in bytes)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    return builder.ToString() == storedHash;
                }
            }

            // Verificación PBKDF2
            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);
                if (hashBytes.Length != (128 / 8) + (256 / 8)) return false;
                
                byte[] salt = new byte[128 / 8];
                Array.Copy(hashBytes, 0, salt, 0, salt.Length);

                using (var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, 100000, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(256 / 8);
                    for (int i = 0; i < hash.Length; i++)
                    {
                        if (hashBytes[i + salt.Length] != hash[i]) return false;
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string CleanRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut)) return "";
            // Soporta guiones, puntos, acentos (lectores de codigo barras mal configurados)
            return Regex.Replace(rut, @"[\.,\s\-Â´'`]", "").ToUpper();
        }

        public static bool ValidaRut(string rut)
        {
            string cleanRut = CleanRut(rut);
            if (cleanRut.Length < 2) return false;

            string dv = cleanRut.Substring(cleanRut.Length - 1);
            string body = cleanRut.Substring(0, cleanRut.Length - 1);

            if (!long.TryParse(body, out long number)) return false;

            int sum = 0;
            int multiplier = 2;

            for (int i = body.Length - 1; i >= 0; i--)
            {
                sum += (body[i] - '0') * multiplier;
                multiplier = multiplier == 7 ? 2 : multiplier + 1;
            }

            int remainder = 11 - (sum % 11);
            string expectedDv = remainder == 11 ? "0" : (remainder == 10 ? "K" : remainder.ToString());

            return dv == expectedDv;
        }
    }
}





