using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AsistenciaDesktop_v2.Config;

namespace AsistenciaDesktop_v2.Data
{
    public static class RemoteDatabaseManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task InitializeDatabaseAsync()
        {
            try
            {
                var payload = "{ \"action\": \"init_db\" }";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.ApiUrl);
                request.Headers.Add("Authorization", "Bearer " + AppConfig.ApiSecret);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 AsistenciaApp/2.0");
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al inicializar base de datos remota vía API: " + ex.Message);
            }
        }
    }
}
