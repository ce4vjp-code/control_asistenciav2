using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string dbPath = "PublishOutput\\asistencia_v2.db";
        if (!File.Exists(dbPath)) {
            Console.WriteLine("DB not found at " + dbPath);
            return;
        }
        string connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Default Timeout=5";
        
        try
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                
                // Check if there's any data
                string hoy = DateTime.Now.ToString("yyyy-MM-dd");
                Console.WriteLine("Checking for date: " + hoy);
                
                cmd.CommandText = @"
                    SELECT a.NumeroUnico, a.Nombre, a.Curso,
                           ac.HoraIngreso, acom.HoraRegistro, ac.HoraSalida
                    FROM Alumnos a
                    LEFT JOIN AsistenciaColegio ac ON a.NumeroUnico = ac.AlumnoId AND ac.Fecha = @fecha
                    LEFT JOIN AsistenciaComedor acom ON a.NumeroUnico = acom.AlumnoId AND acom.Fecha = @fecha
                    WHERE ac.HoraIngreso IS NOT NULL AND ac.HoraIngreso != ''
                      AND (acom.HoraRegistro IS NULL OR acom.HoraRegistro = '')";
                
                cmd.Parameters.AddWithValue("@fecha", hoy);
                
                int count = 0;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        count++;
                        Console.WriteLine("Found: " + reader.GetString(0) + " " + reader.GetString(1));
                    }
                }
                Console.WriteLine("Total matching students: " + count);

                // Check Config
                var cmd2 = conn.CreateCommand();
                cmd2.CommandText = "SELECT Clave, Valor FROM Configuracion";
                using (var reader = cmd2.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("Config: " + reader.GetString(0) + " = " + reader.GetString(1));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
