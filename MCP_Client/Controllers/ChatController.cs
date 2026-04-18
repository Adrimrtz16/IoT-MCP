using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using MCP_Client.DTOs;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly McpClient _client;
    private readonly HttpClient _http = new HttpClient();

    public ChatController(Task<McpClient> clientTask)
    {
        _client = clientTask.Result;
    }

    [HttpGet("tools")]
    public async Task<IActionResult> GetTools()
    {
        var tools = await _client.ListToolsAsync();
        return Ok(tools);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest body)
    {
        string mensaje = body.message;
        var tools = await _client.ListToolsAsync();

        var openaiTools = tools.Select(tool => new
        {
            type = "function",
            function = new
            {
                name = tool.Name,
                description = tool.Description ?? "Sin descripción",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>()
                }
            }
        }).ToList();

        var request = new
        {
            model = "meta-llama-3.1-8b-instruct",
            messages = new[] { new { role = "user", content = mensaje } },
            tools = openaiTools,
            tool_choice = "auto"
        };

        var response = await _http.PostAsJsonAsync(
            "http://localhost:1234/v1/chat/completions",
            request
        );
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (json.TryGetProperty("choices", out var choices))
        {
            var messageModel = choices[0].GetProperty("message");
            if (messageModel.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                var toolName = toolCalls[0]
                    .GetProperty("function")
                    .GetProperty("name")
                    .GetString();

                var resultado = await _client.CallToolAsync(toolName, new Dictionary<string, object?>());

                // 🔥 EXTRAE EL TEXTO DEL RESULTADO
                string toolResult = ExtractToolResultText(resultado);

                return Ok(new
                {
                    accion = toolName,
                    resultado = toolResult,
                    message = toolResult,
                    success = true
                });
            }

            var content = messageModel.GetProperty("content").GetString();
            return Ok(new
            {
                accion = (string)null,
                resultado = (string)null,
                message = !string.IsNullOrEmpty(content)
                    ? content
                    : "No se pudo generar respuesta",
                success = true
            });
        }

        return Ok(new
        {
            accion = (string)null,
            resultado = (string)null,
            message = "Error procesando la respuesta",
            success = false
        });
    }

    // Helper method para extraer el texto
    private string ExtractToolResultText(object resultado)
    {
        try
        {
            // Convertir a JSON element para acceder a las propiedades
            var jsonElement = JsonSerializer.SerializeToElement(resultado);

            if (jsonElement.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array &&
                content.GetArrayLength() > 0)
            {
                var firstContent = content[0];

                if (firstContent.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "Sin texto";
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error extrayendo resultado: {ex.Message}";
        }

        return "No se encontró contenido de texto";
    }

}