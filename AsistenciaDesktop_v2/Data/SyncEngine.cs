using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AsistenciaDesktop_v2.Config;
using System.Text.Json;

namespace AsistenciaDesktop_v2.Data
{
    public static class SyncEngine
    {
        private static Timer _timer;
        private static bool isSyncing = false;
        private static readonly HttpClient _httpClient = new HttpClient();

        public static void StartSyncTimer()
        {
            _timer = new Timer(async (e) => await SyncDataAsync(), null, 0, 30000);
        }

                public static async Task SyncDataAsync()
        {
            if (isSyncing) return;
            isSyncing = true;

            try
            {
                using (var localConn = LocalDatabaseManager.GetConnection())
                {
                    localConn.Open();
                    await SyncAlumnosAsync(localConn);
                    await SyncUsuariosAsync(localConn);
                    await SyncAsistenciaColegioAsync(localConn);
                    await SyncAsistenciaComedorAsync(localConn);
                    await SyncEmailsAsync(localConn);
                    await PullDataAsync(localConn);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sync Engine Error: " + ex.Message);
            }
            finally
            {
                isSyncing = false;
            }
        }

        private static async Task PullDataAsync(SqliteConnection localConn)
        {
            try
            {
                var payload = new { action = "pull_all" };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.ApiUrl);
                request.Headers.Add("Authorization", "Bearer " + AppConfig.ApiSecret);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 AsistenciaApp/2.0");
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    responseString = responseString.TrimStart('\uFEFF');

                    var result = JsonSerializer.Deserialize<JsonElement>(responseString);
                    if (result.TryGetProperty("status", out var status) && status.GetString() == "ok")
                    {
                        var data = result.GetProperty("data");

                        using (var tx = localConn.BeginTransaction())
                        {
                            if (data.TryGetProperty("alumnos", out var alumnos))
                            {
                                var cmd = localConn.CreateCommand();
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO Alumnos (NumeroUnico, Nombre, Curso, EmailApoderado, IsSynced) 
                                                    VALUES (@id, @n, @c, @e, 1)
                                                    ON CONFLICT(NumeroUnico) DO UPDATE SET 
                                                        Nombre = excluded.Nombre,
                                                        Curso = excluded.Curso,
                                                        EmailApoderado = excluded.EmailApoderado
                                                    WHERE IsSynced = 1";
                                cmd.Parameters.Add("@id", SqliteType.Text);
                                cmd.Parameters.Add("@n", SqliteType.Text);
                                cmd.Parameters.Add("@c", SqliteType.Text);
                                cmd.Parameters.Add("@e", SqliteType.Text);
                                foreach (var item in alumnos.EnumerateArray())
                                {
                                    cmd.Parameters["@id"].Value = item.GetProperty("NumeroUnico").GetString();
                                    cmd.Parameters["@n"].Value = item.GetProperty("Nombre").GetString();
                                    cmd.Parameters["@c"].Value = item.GetProperty("Curso").GetString();
                                    cmd.Parameters["@e"].Value = item.GetProperty("EmailApoderado").ValueKind == JsonValueKind.Null ? "" : item.GetProperty("EmailApoderado").GetString();
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (data.TryGetProperty("usuarios", out var usuarios))
                            {
                                var cmd = localConn.CreateCommand();
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO Usuarios (Username, PasswordHash, Role, Email, IsSynced) 
                                                    VALUES (@u, @p, @r, @e, 1)
                                                    ON CONFLICT(Username) DO UPDATE SET 
                                                        PasswordHash = excluded.PasswordHash,
                                                        Role = excluded.Role,
                                                        Email = excluded.Email
                                                    WHERE IsSynced = 1";
                                cmd.Parameters.Add("@u", SqliteType.Text);
                                cmd.Parameters.Add("@p", SqliteType.Text);
                                cmd.Parameters.Add("@r", SqliteType.Text);
                                cmd.Parameters.Add("@e", SqliteType.Text);
                                foreach (var item in usuarios.EnumerateArray())
                                {
                                    cmd.Parameters["@u"].Value = item.GetProperty("Username").GetString();
                                    cmd.Parameters["@p"].Value = item.GetProperty("PasswordHash").GetString();
                                    cmd.Parameters["@r"].Value = item.GetProperty("Role").GetString();
                                    cmd.Parameters["@e"].Value = item.GetProperty("Email").ValueKind == JsonValueKind.Null ? "" : item.GetProperty("Email").GetString();
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (data.TryGetProperty("colegio", out var colegio))
                            {
                                var cmd = localConn.CreateCommand();
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO AsistenciaColegio (AlumnoId, Fecha, HoraIngreso, HoraSalida, IsSynced) 
                                                    VALUES (@id, @f, @hi, @hs, 1)
                                                    ON CONFLICT(AlumnoId, Fecha) DO UPDATE SET 
                                                        HoraIngreso = excluded.HoraIngreso,
                                                        HoraSalida = excluded.HoraSalida
                                                    WHERE IsSynced = 1";
                                cmd.Parameters.Add("@id", SqliteType.Text);
                                cmd.Parameters.Add("@f", SqliteType.Text);
                                cmd.Parameters.Add("@hi", SqliteType.Text);
                                cmd.Parameters.Add("@hs", SqliteType.Text);
                                foreach (var item in colegio.EnumerateArray())
                                {
                                    cmd.Parameters["@id"].Value = item.GetProperty("AlumnoId").GetString();
                                    cmd.Parameters["@f"].Value = item.GetProperty("Fecha").GetString();
                                    cmd.Parameters["@hi"].Value = item.GetProperty("HoraIngreso").GetString();
                                    var hs = item.GetProperty("HoraSalida");
                                    cmd.Parameters["@hs"].Value = hs.ValueKind == JsonValueKind.Null ? DBNull.Value : hs.GetString();
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (data.TryGetProperty("comedor", out var comedor))
                            {
                                var cmd = localConn.CreateCommand();
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO AsistenciaComedor (AlumnoId, Fecha, HoraIngreso, IsSynced) 
                                                    VALUES (@id, @f, @hi, 1)
                                                    ON CONFLICT(AlumnoId, Fecha) DO UPDATE SET 
                                                        HoraIngreso = excluded.HoraIngreso
                                                    WHERE IsSynced = 1";
                                cmd.Parameters.Add("@id", SqliteType.Text);
                                cmd.Parameters.Add("@f", SqliteType.Text);
                                cmd.Parameters.Add("@hi", SqliteType.Text);
                                foreach (var item in comedor.EnumerateArray())
                                {
                                    cmd.Parameters["@id"].Value = item.GetProperty("AlumnoId").GetString();
                                    cmd.Parameters["@f"].Value = item.GetProperty("Fecha").GetString();
                                    cmd.Parameters["@hi"].Value = item.GetProperty("HoraIngreso").GetString();
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Pull Error: " + ex.Message);
            }
        }

        public static async Task<bool> SendApiRequest(string action, object data)
        {
            try
            {
                var payload = new { action = action, data = data };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.ApiUrl);
                request.Headers.Add("Authorization", "Bearer " + AppConfig.ApiSecret);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 AsistenciaApp/2.0");
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task SyncAlumnosAsync(SqliteConnection localConn)
        {
            var cmd = localConn.CreateCommand();
            cmd.CommandText = "SELECT NumeroUnico, Nombre, Curso, EmailApoderado FROM Alumnos WHERE IsSynced = 0";
            
            var list = new List<object>();
            var ids = new List<string>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    ids.Add(id);
                    list.Add(new {
                        NumeroUnico = id,
                        Nombre = reader.GetString(1),
                        Curso = reader.GetString(2),
                        EmailApoderado = reader.IsDBNull(3) ? "" : reader.GetString(3)
                    });
                }
            }

            if (list.Count > 0)
            {
                bool success = await SendApiRequest("sync_alumnos", list);
                if (success)
                {
                    var updateCmd = localConn.CreateCommand();
                    updateCmd.CommandText = "UPDATE Alumnos SET IsSynced = 1 WHERE NumeroUnico = @id";
                    updateCmd.Parameters.Add("@id", SqliteType.Text);
                    
                    foreach(var id in ids) {
                        updateCmd.Parameters["@id"].Value = id;
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static async Task SyncUsuariosAsync(SqliteConnection localConn)
        {
            var cmd = localConn.CreateCommand();
            cmd.CommandText = "SELECT Username, PasswordHash, Role, Email FROM Usuarios WHERE IsSynced = 0";
            
            var list = new List<object>();
            var ids = new List<string>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string u = reader.GetString(0);
                    ids.Add(u);
                    list.Add(new {
                        Username = u,
                        PasswordHash = reader.GetString(1),
                        Role = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? "" : reader.GetString(3)
                    });
                }
            }

            if (list.Count > 0)
            {
                bool success = await SendApiRequest("sync_usuarios", list);
                if (success)
                {
                    var updateCmd = localConn.CreateCommand();
                    updateCmd.CommandText = "UPDATE Usuarios SET IsSynced = 1 WHERE Username = @u";
                    updateCmd.Parameters.Add("@u", SqliteType.Text);
                    foreach(var id in ids) {
                        updateCmd.Parameters["@u"].Value = id;
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

                private static async Task SyncAsistenciaColegioAsync(SqliteConnection localConn)
        {
            var cmd = localConn.CreateCommand();
            cmd.CommandText = "SELECT AlumnoId, Fecha, HoraIngreso, HoraSalida FROM AsistenciaColegio WHERE IsSynced = 0";
            
            var list = new List<object>();
            var records = new List<Tuple<string, string, string, string>>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string fecha = reader.GetString(1);
                    string hi = reader.GetString(2);
                    string hs = reader.IsDBNull(3) ? "" : reader.GetString(3);

                    records.Add(new Tuple<string, string, string, string>(id, fecha, hi, hs));
                    
                    list.Add(new {
                        AlumnoId = id,
                        Fecha = fecha,
                        HoraIngreso = hi,
                        HoraSalida = hs
                    });
                }
            }

            if (list.Count > 0)
            {
                bool success = await SendApiRequest("sync_asistencia_colegio", list);
                if (success)
                {
                    var updateCmd = localConn.CreateCommand();
                    updateCmd.CommandText = "UPDATE AsistenciaColegio SET IsSynced = 1 WHERE AlumnoId = @id AND Fecha = @f AND HoraIngreso = @hi AND (HoraSalida = @hs OR (HoraSalida IS NULL AND @hs = ''))";
                    updateCmd.Parameters.Add("@id", SqliteType.Text);
                    updateCmd.Parameters.Add("@f", SqliteType.Text);
                    updateCmd.Parameters.Add("@hi", SqliteType.Text);
                    updateCmd.Parameters.Add("@hs", SqliteType.Text);
                    foreach(var r in records) {
                        updateCmd.Parameters["@id"].Value = r.Item1;
                        updateCmd.Parameters["@f"].Value = r.Item2;
                        updateCmd.Parameters["@hi"].Value = r.Item3;
                        updateCmd.Parameters["@hs"].Value = r.Item4;
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }






        private static async Task SyncAsistenciaComedorAsync(SqliteConnection localConn)
        {
            var cmd = localConn.CreateCommand();
            cmd.CommandText = "SELECT AlumnoId, Fecha, HoraIngreso FROM AsistenciaComedor WHERE IsSynced = 0";
            
            var list = new List<object>();
            var records = new List<Tuple<string, string, string>>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string fecha = reader.GetString(1);
                    string hi = reader.GetString(2);
                    records.Add(new Tuple<string, string, string>(id, fecha, hi));
                    
                    list.Add(new {
                        AlumnoId = id,
                        Fecha = fecha,
                        HoraIngreso = hi
                    });
                }
            }

            if (list.Count > 0)
            {
                bool success = await SendApiRequest("sync_asistencia_comedor", list);
                if (success)
                {
                    var updateCmd = localConn.CreateCommand();
                    updateCmd.CommandText = "UPDATE AsistenciaComedor SET IsSynced = 1 WHERE AlumnoId = @id AND Fecha = @f AND HoraIngreso = @hi";
                    updateCmd.Parameters.Add("@id", SqliteType.Text);
                    updateCmd.Parameters.Add("@f", SqliteType.Text);
                    updateCmd.Parameters.Add("@hi", SqliteType.Text);
                    foreach(var r in records) {
                        updateCmd.Parameters["@id"].Value = r.Item1;
                        updateCmd.Parameters["@f"].Value = r.Item2;
                        updateCmd.Parameters["@hi"].Value = r.Item3;
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }
        private static async Task SyncEmailsAsync(SqliteConnection localConn)
        {
            var cmd = localConn.CreateCommand();
            cmd.CommandText = "SELECT Id, ToEmail, Subject, Body, PdfBase64, PdfName FROM EmailsQueue LIMIT 5";
            
            var records = new List<Tuple<int, string, string, string, string, string>>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string to = reader.GetString(1);
                    string subj = reader.GetString(2);
                    string body = reader.GetString(3);
                    string pdfB = reader.IsDBNull(4) ? null : reader.GetString(4);
                    string pdfN = reader.IsDBNull(5) ? null : reader.GetString(5);
                    records.Add(new Tuple<int, string, string, string, string, string>(id, to, subj, body, pdfB, pdfN));
                }
            }

            foreach(var r in records) {
                var payload = new {
                    to = r.Item2,
                    subject = r.Item3,
                    body = r.Item4,
                    pdf_base64 = r.Item5,
                    pdf_name = r.Item6
                };
                
                bool success = await SendApiRequest("send_email", payload);
                if (success) {
                    var delCmd = localConn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM EmailsQueue WHERE Id = @id";
                    delCmd.Parameters.AddWithValue("@id", r.Item1);
                    delCmd.ExecuteNonQuery();
                }
            }
        }
    }
}

