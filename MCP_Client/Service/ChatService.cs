using ModelContextProtocol.Client;
using System.Text.Json;

public class ChatService
{
    private readonly McpClient _mcpClient;
    private readonly HttpClient _httpClient;
    private readonly string _lmStudioUrl = "http://localhost:1234/v1/chat/completions";

    public ChatService(McpClient mcpClient)
    {
        _mcpClient = mcpClient;
        _httpClient = new HttpClient();
    }

    public async Task<ChatResponse> ProcessMessageAsync(string userMessage)
    {
        // 1️⃣ Obtener tools disponibles del MCP Server
        var tools = await _mcpClient.ListToolsAsync();
        var toolDescriptions = BuildToolDescriptions(tools);

        // 2️⃣ Enviar al LM Studio con contexto de tools disponibles
        var llmResponse = await CallLmStudio(userMessage, toolDescriptions);

        // 3️⃣ Analizar si el LM Studio sugiere una tool
        var toolName = ExtractToolName(llmResponse.Content);

        if (!string.IsNullOrEmpty(toolName))
        {
            // 4️⃣ Ejecutar la tool a través del MCP Server
            var toolResult = await _mcpClient.CallToolAsync(toolName, new Dictionary<string, object?>());
            var toolResultText = ExtractToolResultText(toolResult);

            return new ChatResponse
            {
                Action = toolName,
                Result = toolResultText,
                Message = toolResultText,
                Success = true
            };
        }

        // Si no hay tool, devolver respuesta del LM
        return new ChatResponse
        {
            Action = null,
            Result = null,
            Message = llmResponse.Content,
            Success = true
        };
    }

    // 🛠️ Construir descripciones de tools para el LM Studio
    // Usa IEnumerable para ser flexible con el tipo exacto
    private string BuildToolDescriptions(IEnumerable<dynamic> tools)
    {
        var descriptions = new List<string>();
        foreach (var tool in tools)
        {
            try
            {
                string name = tool.Name;
                string description = tool.Description ?? "Sin descripción";
                descriptions.Add($"- {name}: {description}");
            }
            catch
            {
                // Si hay algún error al acceder a las propiedades, ignoralo
                continue;
            }
        }

        return "Herramientas disponibles:\n" + string.Join("\n", descriptions);
    }

    // 📡 Llamar a LM Studio
    private async Task<LmStudioResponse> CallLmStudio(string userMessage, string toolDescriptions)
    {
        var request = new
        {
            model = "meta-llama-3.1-8b-instruct",
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = $@"Eres un asistente inteligente que controla un sistema de luces.

{toolDescriptions}

Cuando el usuario pida realizar una acción, analiza si alguna de las herramientas disponibles puede ayudar.
Si es apropiado, responde con el nombre de la herramienta precedido por 'TOOL:' al inicio de tu respuesta.

Ejemplo:
- Usuario: ""Enciende la luz roja""
- Tu respuesta: ""TOOL:EncenderLuzRoja""

Si no es posible realizar la acción o el usuario solo pide información, responde normalmente sin mencionar herramientas."
                },
                new
                {
                    role = "user",
                    content = userMessage
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_lmStudioUrl, request);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var content = json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Error al procesar respuesta";

            return new LmStudioResponse { Content = content };
        }
        catch (Exception ex)
        {
            return new LmStudioResponse { Content = $"Error conectando con LM Studio: {ex.Message}" };
        }
    }

    // 🔍 Extraer nombre de tool
    private string? ExtractToolName(string content)
    {
        const string toolPrefix = "TOOL:";
        int index = content.IndexOf(toolPrefix, StringComparison.OrdinalIgnoreCase);

        if (index >= 0)
        {
            string remainder = content.Substring(index + toolPrefix.Length).Trim();
            string toolName = remainder.Split(new[] { '\n', ' ', ':' }, StringSplitOptions.None)[0];
            return string.IsNullOrWhiteSpace(toolName) ? null : toolName;
        }

        return null;
    }

    // 📦 Extraer resultado de tool
    private string ExtractToolResultText(object resultado)
    {
        try
        {
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

// 📋 Modelos
public class ChatResponse
{
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string Message { get; set; } = "";
    public bool Success { get; set; }
}

public class LmStudioResponse
{
    public string Content { get; set; } = "";
}