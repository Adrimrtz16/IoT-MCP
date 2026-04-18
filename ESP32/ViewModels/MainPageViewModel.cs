using System.Net.Http.Json;
using ESP32.Models;

namespace ESP32.ViewModels
{
    public class MainPageViewModel
    {
        private readonly HttpClient _httpClient = new HttpClient();

        // Evento para notificar a la vista que agregue un mensaje
        public event Action<string, bool>? OnMessageReceived;

        public async void OnSendClicked(string messageText, Action<string> clearMessage)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            // Notificar a la vista que muestre el mensaje del usuario
            OnMessageReceived?.Invoke(messageText, true);
            clearMessage(string.Empty);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5256/api/chat", new
                {
                    message = messageText
                });

                if (response.IsSuccessStatusCode)
                {
                    var resultado = await response.Content.ReadFromJsonAsync<ChatResponse>();
                    if (resultado != null && !string.IsNullOrEmpty(resultado.message))
                    {
                        OnMessageReceived?.Invoke(resultado.message, false);
                    }
                    else if (resultado != null)
                    {
                        string fallbackMessage = !string.IsNullOrEmpty(resultado.accion)
                            ? $"Se ejecutó: {resultado.accion}"
                            : "Acción completada";
                        OnMessageReceived?.Invoke(fallbackMessage, false);
                    }
                }
                else
                {
                    OnMessageReceived?.Invoke("Error en la respuesta del servidor.", false);
                }
            }
            catch (Exception ex)
            {
                OnMessageReceived?.Invoke($"Error al conectar: {ex.Message}", false);
            }
        }
    }
}