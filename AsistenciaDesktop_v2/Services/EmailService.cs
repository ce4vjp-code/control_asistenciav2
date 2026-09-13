using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using AsistenciaDesktop_v2.Data;
using Microsoft.Data.Sqlite;

namespace AsistenciaDesktop_v2.Services
{
    public static class EmailService
    {
        public static async Task EnviarNotificacionIngresoAsync(string emailDestino, string nombreAlumno, string horaIngreso)
        {
            if (string.IsNullOrWhiteSpace(emailDestino)) return;

            try
            {
                string safeNombre = WebUtility.HtmlEncode(nombreAlumno);
                string safeHora = WebUtility.HtmlEncode(horaIngreso);

                string body = $@"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                        <div style='background-color: #3F51B5; padding: 20px; text-align: center;'>
                            <h2 style='color: white; margin: 0;'>Control de Asistencia</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Estimado Apoderado,</p>
                            <p>Le informamos que el alumno/a <strong>{safeNombre}</strong> ha registrado su ingreso al establecimiento de forma exitosa.</p>
                            <p><strong>Hora de Ingreso:</strong> {safeHora}</p>
                            <br/>
                            <p style='color: #666; font-size: 12px;'>Este es un mensaje generado automáticamente. Por favor no responda a este correo.</p>
                        </div>
                    </div>";

                QueueEmail(emailDestino, $"Notificación de Ingreso - {safeNombre}", body, null, null);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al encolar email de ingreso: {ex.Message}");
            }
        }

        public static async Task EnviarReporteAdminsAsync(System.Collections.Generic.List<string> correosAdmins, string pdfPath, string fecha)
        {
            if (correosAdmins == null || correosAdmins.Count == 0 || !System.IO.File.Exists(pdfPath)) return;

            try
            {
                string pdfBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(pdfPath));
                string pdfName = System.IO.Path.GetFileName(pdfPath);
                
                string body = $"Estimado Administrador,<br><br>Adjunto se encuentra el reporte de asistencia correspondiente al día {WebUtility.HtmlEncode(fecha)}.<br><br>Saludos cordiales,<br>Sistema de Control de Asistencia";

                foreach (var email in correosAdmins)
                {
                    if (string.IsNullOrWhiteSpace(email)) continue;
                    QueueEmail(email, $"Reporte de Asistencia Diaria - {fecha}", body, pdfBase64, pdfName);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al encolar reporte PDF: {ex.Message}");
            }
        }

        private static void QueueEmail(string to, string subject, string body, string pdfBase64, string pdfName)
        {
            using (var conn = LocalDatabaseManager.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO EmailsQueue (ToEmail, Subject, Body, PdfBase64, PdfName) VALUES (@t, @s, @b, @pb, @pn)";
                cmd.Parameters.AddWithValue("@t", to);
                cmd.Parameters.AddWithValue("@s", subject);
                cmd.Parameters.AddWithValue("@b", body);
                cmd.Parameters.AddWithValue("@pb", string.IsNullOrEmpty(pdfBase64) ? DBNull.Value : (object)pdfBase64);
                cmd.Parameters.AddWithValue("@pn", string.IsNullOrEmpty(pdfName) ? DBNull.Value : (object)pdfName);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
