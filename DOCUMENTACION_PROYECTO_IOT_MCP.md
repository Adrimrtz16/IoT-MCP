# 📚 Documentación Completa: Sistema IoT con Model Context Protocol (MCP)

**Autor:** Proyecto ESP32 + MCP  
**Fecha:** 2026  
**Versión:** 1.0  
**Ubicación:** Cartagena, Murcia, España

---

## 📋 Tabla de Contenidos

1. [Arquitectura General](#arquitectura-general)
2. [Componentes del Sistema](#componentes-del-sistema)
3. [Arduino/ESP32](#arduinoesp32)
4. [MCP Server](#mcp-server)
5. [MCP Client](#mcp-client)
6. [LM Studio](#lm-studio)
7. [Node-RED](#node-red)
8. [MAUI (Frontend)](#maui-frontend)
9. [Flujo de Comunicación](#flujo-de-comunicación)
10. [Instrucciones de Instalación](#instrucciones-de-instalación)
11. [Solución de Problemas](#solución-de-problemas)

---

## 🏗️ Arquitectura General

Este proyecto implementa un **sistema inteligente de control de luces** que combina:

- **ESP32** con sensores y actuadores (LEDs RGB, buzzer)
- **MQTT** para comunicación IoT
- **Node-RED** como intermediario de automatización
- **MCP Server** que expone herramientas (tools) para control de luces
- **LM Studio** (LLM local) para procesamiento de lenguaje natural
- **MCP Client** que consume las herramientas del MCP Server
- **MAUI** aplicación móvil/escritorio para interfaz de usuario

### Diagrama de Flujo

```
┌─────────────┐
│   Usuario   │
│  (MAUI App) │
└──────┬──────┘
       │ HTTP POST
       ↓
┌──────────────────────┐
│  MCP Client (API)    │
│  - ChatController    │
│  - ChatService       │
└──────┬───────────────┘
       │ HTTP (via LM Studio)
       ↓
┌──────────────────────┐
│   LM Studio (LLM)    │
│ meta-llama-3.1-8b    │
└──────┬───────────────┘
       │ Interpreta intención
       ↓
┌──────────────────────┐
│   MCP Server         │
│ - Tools (Luces)      │
└──────┬───────────────┘
       │ HTTP POST
       ↓
┌──────────────────────┐
│   Node-RED           │
│   /luz endpoint      │
└──────┬───────────────┘
       │ MQTT (tarea7/luces)
       ↓
┌──────────────────────┐
│  ESP32 + Hardware    │
│ - LEDs RGB           │
│ - Buzzer             │
│ - Display OLED       │
└──────────────────────┘
```

---

## 🔧 Componentes del Sistema

### Hardware

| Componente | Especificaciones | GPIO/Pin |
|-----------|-----------------|----------|
| **LED Rojo** | Diodo LED 5mm | GPIO 18 |
| **LED Verde** | Diodo LED 5mm | GPIO 21 |
| **LED Amarillo** | Diodo LED 5mm | GPIO 19 |
| **Buzzer** | Buzzer pasivo | GPIO 2 |
| **Display OLED** | SSD1306 128x64 | I2C (GPIO 23, 22) |
| **ESP32** | Microcontrolador WiFi | - |

### Software

| Componente | Tecnología | Versión |
|-----------|-----------|---------|
| **Arduino IDE** | C++ | ESP32 |
| **MCP Server** | .NET 6+ | C# |
| **MCP Client** | ASP.NET Core | C# |
| **LM Studio** | Local LLM | meta-llama-3.1-8b-instruct |
| **Node-RED** | JavaScript | - |
| **MAUI** | .NET MAUI | C# |
| **MQTT Broker** | Mosquitto | test.mosquitto.org:1883 |

---

## 🔌 Arduino/ESP32

### 📄 Descripción General

El código del ESP32 implementa un **cliente MQTT** que:
- Se conecta a la red WiFi
- Se suscribe al topic `tarea7/luces`
- Recibe comandos para encender LEDs de diferentes colores
- Genera sonidos característicos con el buzzer
- Muestra feedback visual en pantalla OLED

### 📝 Código Completo

```cpp
#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// ========== CONFIGURACIÓN WiFi ==========
const char* SSID = "ESP32TEST";
const char* PASSWORD = "12345678";

// ========== CONFIGURACIÓN MQTT ==========
const char* MQTT_SERVER = "test.mosquitto.org";
const int MQTT_PORT = 1883;
const char* MQTT_TOPIC = "tarea7/luces";

// ========== PINES ==========
const int LED_ROJO = 18;
const int LED_VERDE = 21;
const int LED_AMARILLO = 19;
const int BUZZER = 2;

// ========== OLED ==========
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, -1);

// ========== CLIENTES ==========
WiFiClient espClient;
PubSubClient client(espClient);

// ========== SONIDOS ==========

void sonidoVerde() {
  // Seta / Power-Up (Arpegios rápidos ascendentes)
  int notas[] = {
    392, 523, 659, 784, 1047, 1319, 
    415, 554, 698, 831, 1109, 1397, 
    466, 622, 784, 932, 1245, 1568
  };
  for (int i = 0; i < 18; i++) {
    tone(BUZZER, notas[i]);
    delay(35);
  }
  noTone(BUZZER);
}

void sonidoRojo() {
  // Daño / Encoger (Escala rápida descendente)
  int notas[] = {659, 523, 494, 392, 330, 262}; 
  for(int i = 0; i < 6; i++){
    tone(BUZZER, notas[i]);
    delay(60);
  }
  tone(BUZZER, 131); // Nota grave final
  delay(200);
  noTone(BUZZER);
}

void sonidoAmarillo() {
  // Estrella de invencibilidad
  int notas[] = {523, 523, 523, 392, 523, 587, 494, 494, 494, 392, 494, 523};
  int duraciones[] = {100, 100, 100, 100, 100, 150, 100, 100, 100, 100, 100, 150};
  
  for(int i = 0; i < 12; i++){
    tone(BUZZER, notas[i]);
    delay(duraciones[i]);
    noTone(BUZZER);
    delay(20);
  }
}

// ========== CONTROL DE LUCES ==========

void apagarTodos() {
  digitalWrite(LED_ROJO, LOW);
  digitalWrite(LED_VERDE, LOW);
  digitalWrite(LED_AMARILLO, LOW);
}

void encenderLed(String color) {
  apagarTodos();

  if (color == "rojo") {
    Serial.println("Encendiendo LED ROJO");
    digitalWrite(LED_ROJO, HIGH);
    sonidoRojo();
  } 
  else if (color == "verde") {
    Serial.println("Encendiendo LED VERDE");
    digitalWrite(LED_VERDE, HIGH);
    sonidoVerde();
  } 
  else if (color == "amarillo") {
    Serial.println("Encendiendo LED AMARILLO");
    digitalWrite(LED_AMARILLO, HIGH);
    sonidoAmarillo();
  }

  mostrarColorPantalla(color);
}

// ========== CALLBACK MQTT ==========

void callback(char* topic, byte* message, unsigned int length) {
  String msg;

  for (int i = 0; i < length; i++) {
    msg += (char)message[i];
  }

  Serial.print("Mensaje recibido: ");
  Serial.println(msg);

  if (msg.startsWith("encender_")) {
    String color = msg.substring(9); // Extrae el color
    encenderLed(color);
  }
}

// ========== CONEXIÓN WiFi ==========

void conectarWiFi() {
  Serial.println("Conectando a WiFi...");
  WiFi.begin(SSID, PASSWORD);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi conectado");
}

// ========== CONEXIÓN MQTT ==========

void conectarMQTT() {
  while (!client.connected()) {
    Serial.println("Conectando a MQTT...");

    String clientId = "ESP32-" + String((uint32_t)ESP.getEfuseMac());

    if (client.connect(clientId.c_str())) {
      Serial.println("Conectado a MQTT");
      client.subscribe(MQTT_TOPIC);
    } else {
      Serial.print("Error MQTT, rc=");
      Serial.print(client.state());
      Serial.println(" reintentando...");
      delay(2000);
    }
  }
}

// ========== PANTALLA OLED ==========

void mostrarColorPantalla(String color) {
  display.clearDisplay();

  display.setTextSize(1);
  display.setTextColor(WHITE);
  display.setCursor(0, 0);
  display.println("Se ha encendido");

  display.setCursor(0, 10);
  display.println("el color: " + color);

  display.display();
}

// ========== SETUP ==========

void setup() {
  pinMode(BUZZER, OUTPUT);

  // Inicializar I2C para OLED
  Wire.begin(23, 22);

  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
    Serial.println("Error OLED");
    while(true);
  }

  display.clearDisplay();
  display.display();

  Serial.begin(115200);
  delay(1000);

  Serial.println("Iniciando...");

  // Configurar pines LED
  pinMode(LED_ROJO, OUTPUT);
  pinMode(LED_VERDE, OUTPUT);
  pinMode(LED_AMARILLO, OUTPUT);

  // Configurar WiFi
  WiFi.mode(WIFI_STA);
  WiFi.disconnect();
  delay(100);

  conectarWiFi();

  // Configurar MQTT
  client.setServer(MQTT_SERVER, MQTT_PORT);
  client.setCallback(callback);
  client.setKeepAlive(60);
}

// ========== LOOP ==========

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    conectarWiFi();
  }

  if (!client.connected()) {
    conectarMQTT();
  }

  client.loop();
}
```

### ⚙️ Configuración Detallada

#### WiFi
- **SSID:** `ESP32TEST`
- **Contraseña:** `12345678`
- **Modo:** STA (Station)

#### MQTT
- **Servidor:** `test.mosquitto.org`
- **Puerto:** `1883`
- **Topic:** `tarea7/luces`
- **Formato Mensaje:** `encender_[color]`
  - `encender_rojo` → LED Rojo
  - `encender_verde` → LED Verde
  - `encender_amarillo` → LED Amarillo

#### Pines GPIO
```
GPIO 18 → LED Rojo
GPIO 21 → LED Verde
GPIO 19 → LED Amarillo
GPIO 2  → Buzzer
GPIO 23 → I2C SDA (OLED)
GPIO 22 → I2C SCL (OLED)
```

### 🎵 Sonidos Implementados

| Color | Tipo | Descripción |
|-------|------|-------------|
| 🟢 Verde | Power-Up | Arpegios ascendentes rápidos |
| 🔴 Rojo | Daño | Escala descendente + nota grave |
| 🟡 Amarillo | Invencibilidad | Fragmento de Super Mario |

---

## 🖥️ MCP Server

### 📄 Descripción General

El **MCP Server** es una aplicación .NET que:
- Expone herramientas (tools) para controlar las luces
- Se comunica con Node-RED mediante HTTP
- Funciona como intermediario entre el MCP Client y el hardware

### 📝 Código Completo (Program.cs)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

// Registrar MCP Server con transporte stdio
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

// ========== DEFINICIÓN DE TOOLS ==========

[McpServerToolType]
public class Tools
{
    static HttpClient client = new HttpClient();

    [McpServerTool, Description("Enciende la luz roja")] 
    public static async Task<string> EncenderLuzRoja()
    {
        await Enviar("encender_rojo");
        return "Luz roja encendida";
    }

    [McpServerTool, Description("Enciende la luz verde")]
    public static async Task<string> EncenderLuzVerde()
    {
        await Enviar("encender_verde");
        return "Luz verde encendida";
    }

    [McpServerTool, Description("Enciende la luz amarilla")]
    public static async Task<string> EncenderLuzAmarilla()
    {
        await Enviar("encender_amarillo");
        return "Luz amarilla encendida";
    }

    [McpServerTool, Description("Llamar cuando el usuario pide algo que no se puede realizar")]
    public static async Task<string> NoAction()
    {
        return $"No estoy programado para hacer eso";
    }

    [McpServerTool, Description("Lista todas las herramientas disponibles y explica qué pueden hacer")]
    public static async Task<string> ListarTools()
    {
        var tools = new[]
        {
            "encender_luz_roja - Enciende la luz roja",
            "encender_luz_amarilla - Enciende la luz amarilla",
            "encender_luz_verde - Enciende la luz verde",
            "no_action - Se llama cuando no se puede realizar una acción",
            "listar_tools - Muestra todas las herramientas disponibles"
        };

        return "Herramientas disponibles:\n" + string.Join("\n", tools);
    }

    // ========== MÉTODO AUXILIAR ==========

    static async Task Enviar(string payload)
    {
        var content = new StringContent(payload, Encoding.UTF8);
        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        try
        {
            await client.PostAsync("http://127.0.0.1:1880/luz", content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando a Node-RED: {ex.Message}");
        }
    }
}
```

### 🔧 Configuración

#### Transporte
- **Tipo:** Stdio (comunicación vía entrada/salida estándar)
- **Cliente:** Acceso mediante StdioClientTransport desde el MCP Client

#### Tools Disponibles

| Nombre | Descripción | Acción |
|--------|-------------|--------|
| `EncenderLuzRoja` | Enciende el LED rojo | POST a `/luz` con `encender_rojo` |
| `EncenderLuzVerde` | Enciende el LED verde | POST a `/luz` con `encender_verde` |
| `EncenderLuzAmarilla` | Enciende el LED amarillo | POST a `/luz` con `encender_amarillo` |
| `NoAction` | Acción por defecto | Respuesta de rechazo |
| `ListarTools` | Listar herramientas | Enumera todas las tools |

#### Endpoint Node-RED
- **URL:** `http://127.0.0.1:1880/luz`
- **Método:** POST
- **Content-Type:** `text/plain`
- **Payload:** `encender_[color]`

---

## 💻 MCP Client

### 📄 Descripción General

El **MCP Client** es una API REST en ASP.NET Core que:
- Conecta con el MCP Server mediante Stdio
- Procesa mensajes de chat del usuario
- Envía contexto de herramientas a LM Studio
- Interpreta respuestas del LLM
- Ejecuta herramientas cuando es necesario

### 📝 Código: Program.cs (Startup)

```csharp
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ========== REGISTRAR MCP CLIENT ==========

builder.Services.AddSingleton<Task<McpClient>>(async _ =>
{
    var transport = new StdioClientTransport(new StdioClientTransportOptions
    {
        Command = "dotnet",
        Arguments = new[]
        {
            "run",
            "--project",
            "C:/Users/jamar/source/repos/ESP32/MCP_Server/MCP_Server.csproj"
        }
    });

    return await McpClient.CreateAsync(transport);
});

// ========== REGISTRAR CHAT SERVICE ==========

builder.Services.AddScoped<ChatService>(provider =>
{
    var mcpClientTask = provider.GetRequiredService<Task<McpClient>>();
    var mcpClient = mcpClientTask.Result;
    return new ChatService(mcpClient);
});

var app = builder.Build();

app.MapControllers();

app.Run();
```

### 📝 Código: ChatController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using MCP_Client.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly McpClient _client;
    private readonly ChatService _chatService;

    public ChatController(Task<McpClient> clientTask, ChatService chatService)
    {
        _client = clientTask.Result;
        _chatService = chatService;
    }

    // ========== OBTENER HERRAMIENTAS DISPONIBLES ==========

    [HttpGet("tools")]
    public async Task<IActionResult> GetTools()
    {
        var tools = await _client.ListToolsAsync();
        return Ok(tools);
    }

    // ========== PROCESAR MENSAJE DE CHAT ==========

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest body)
    {
        var response = await _chatService.ProcessMessageAsync(body.message);
        return Ok(response);
    }
}
```

### 📝 Código: ChatService.cs

```csharp
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

    // ========== PROCESAR MENSAJE ==========

    public async Task<ChatResponse> ProcessMessageAsync(string userMessage)
    {
        // 1️⃣ Obtener tools disponibles del MCP Server
        var tools = await _mcpClient.ListToolsAsync();
        var toolDescriptions = BuildToolDescriptions(tools);

        // 2️⃣ Enviar al LM Studio con contexto de tools
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

    // ========== CONSTRUIR DESCRIPCIONES DE TOOLS ==========

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
                continue;
            }
        }

        return "Herramientas disponibles:\n" + string.Join("\n", descriptions);
    }

    // ========== LLAMAR A LM STUDIO ==========

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

    // ========== EXTRAER NOMBRE DE TOOL ==========

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

    // ========== EXTRAER RESULTADO DE TOOL ==========

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

// ========== MODELOS ==========

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
```

### 📡 Endpoints

#### GET /api/chat/tools
Obtiene lista de herramientas disponibles del MCP Server.

**Respuesta:**
```json
[
  {
    "name": "EncenderLuzRoja",
    "description": "Enciende la luz roja"
  },
  {
    "name": "EncenderLuzVerde",
    "description": "Enciende la luz verde"
  },
  ...
]
```

#### POST /api/chat
Procesa un mensaje de usuario y ejecuta la acción correspondiente.

**Request:**
```json
{
  "message": "Enciende la luz roja"
}
```

**Respuesta:**
```json
{
  "action": "EncenderLuzRoja",
  "result": "Luz roja encendida",
  "message": "Luz roja encendida",
  "success": true
}
```

### 🔄 Flujo de Procesamiento

1. **Usuario envía mensaje** → `POST /api/chat`
2. **ChatService obtiene tools** → `_mcpClient.ListToolsAsync()`
3. **Envía a LM Studio** → Con descripción de tools disponibles
4. **LM Studio interpreta** → Responde con `TOOL:NombreTool` si aplica
5. **ChatService extrae nombre** → Busca "TOOL:" en respuesta
6. **Ejecuta herramienta** → `_mcpClient.CallToolAsync(toolName)`
7. **MCP Server envía HTTP** → POST a Node-RED `/luz`
8. **Retorna resultado** → Cliente recibe respuesta de la acción

### 🔌 Configuración

#### Puerto
- **Predeterminado:** `5256`
- **URL Base:** `http://localhost:5256/api/chat`

#### MCP Server
- **Transporte:** Stdio
- **Comando:** `dotnet run --project C:/Users/jamar/source/repos/ESP32/MCP_Server/MCP_Server.csproj`

#### LM Studio
- **URL:** `http://localhost:1234/v1/chat/completions`
- **Modelo:** `meta-llama-3.1-8b-instruct`

---

## 🤖 LM Studio

### 📄 Descripción General

**LM Studio** es un servidor de LLM local que:
- Ejecuta el modelo `meta-llama-3.1-8b-instruct`
- Procesa solicitudes HTTP en el puerto `1234`
- Interpreta intenciones del usuario basadas en tools disponibles
- No almacena datos ni requiere API externa

### ⚙️ Configuración

#### Instalación

1. Descargar LM Studio desde https://lmstudio.ai
2. Instalar el modelo `meta-llama-3.1-8b-instruct` en la interfaz
3. Iniciar el servidor local

#### Parámetros del Servidor

```
URL: http://localhost:1234/v1/chat/completions
Método: POST
Content-Type: application/json
```

#### Request JSON

```json
{
  "model": "meta-llama-3.1-8b-instruct",
  "messages": [
    {
      "role": "system",
      "content": "Eres un asistente que controla luces. Tools: encender_luz_roja, encender_luz_verde, encender_luz_amarilla. Si el usuario pide una acción, responde con TOOL:NombreHerramienta"
    },
    {
      "role": "user",
      "content": "Enciende la luz roja"
    }
  ]
}
```

#### Response JSON

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "TOOL:EncenderLuzRoja"
      }
    }
  ]
}
```

### 🧠 Prompt del Sistema

El sistema prompt enviado a LM Studio es:

```
Eres un asistente inteligente que controla un sistema de luces.

Herramientas disponibles:
- EncenderLuzRoja: Enciende la luz roja
- EncenderLuzVerde: Enciende la luz verde
- EncenderLuzAmarilla: Enciende la luz amarilla
- NoAction: Se llama cuando no se puede realizar una acción
- ListarTools: Muestra todas las herramientas disponibles

Cuando el usuario pida realizar una acción, analiza si alguna de las herramientas disponibles puede ayudar.
Si es apropiado, responde con el nombre de la herramienta precedido por 'TOOL:' al inicio de tu respuesta.

Ejemplo:
- Usuario: "Enciende la luz roja"
- Tu respuesta: "TOOL:EncenderLuzRoja"

Si no es posible realizar la acción o el usuario solo pide información, responde normalmente sin mencionar herramientas.
```

### 📋 Ejemplos de Uso

| Usuario | LLM Responde | Acción |
|---------|-------------|--------|
| "Enciende la luz roja" | `TOOL:EncenderLuzRoja` | Ejecuta herramienta |
| "Quiero verde" | `TOOL:EncenderLuzVerde` | Ejecuta herramienta |
| "Pon amarillo" | `TOOL:EncenderLuzAmarilla` | Ejecuta herramienta |
| "¿Qué herramientas tienes?" | `TOOL:ListarTools` | Muestra herramientas |
| "Hola, ¿cómo estás?" | "Hola, soy un asistente..." | Sin ejecución |

---

## 🔴 Node-RED

### 📄 Descripción General

**Node-RED** es un entorno de automatización que:
- Recibe solicitudes HTTP del MCP Server
- Publica mensajes MQTT hacia el ESP32
- Actúa como intermediario entre software y hardware
- Permite visualizar el flujo de datos en tiempo real

### 🏗️ Arquitectura en Node-RED

```
┌──────────────────────┐
│   HTTP Request       │
│  POST /luz           │
│  Payload: text/plain │
└──────────┬───────────┘
           │
           ↓
┌──────────────────────┐
│  Validar Payload     │
│  - encender_rojo     │
│  - encender_verde    │
│  - encender_amarillo │
└──────────┬───────────┘
           │
           ↓
┌──────────────────────┐
│   MQTT Publish       │
│   Topic:             │
│   tarea7/luces       │
└──────────┬───────────┘
           │
           ↓
┌──────────────────────┐
│  Mosquitto Broker    │
│ test.mosquitto.org   │
│       :1883          │
└──────────┬───────────┘
           │
           ↓
┌──────────────────────┐
│   ESP32 Subscriber   │
│   Recibe mensaje     │
│   Ejecuta acción     │
└──────────────────────┘
```

### 🔧 Configuración del Endpoint HTTP

#### Nodo HTTP Input
- **Método:** POST
- **URL:** `/luz`
- **Puerto:** 1880 (default)

#### Nodo MQTT Output
- **Servidor:** `test.mosquitto.org`
- **Puerto:** 1883
- **Topic:** `tarea7/luces`
- **QoS:** 1 (At least once)

### 📊 Propiedades MQTT en Node-RED

Según la captura (Image 1), la configuración incluye:

| Propiedad | Valor |
|-----------|-------|
| **Servidor** | test.mosquitto.org:1883 |
| **Tema** | tarea7/luces |
| **CdS** | (Quality of Service) |
| **Retener** | (Retain message) |

### 💡 Recomendación del Sistema

> "Deja el tema, CdS o mantenngalo en blanco si quieres configurarlos a través de las propiedades del mensaje"

Esto significa que puedes:
1. Dejar valores en blanco y configurar dinámicamente en el flujo
2. Usar valores fijos en la configuración del nodo
3. Mezclar ambos enfoques según necesidad

---

## 📱 MAUI (Frontend)

### 📄 Descripción General

**MAUI** es la aplicación cliente que:
- Proporciona interfaz gráfica para el usuario
- Envía mensajes al MCP Client mediante HTTP
- Muestra respuestas y acciones ejecutadas
- Funciona en múltiples plataformas (Windows, macOS, iOS, Android)

### 📝 Código: MainPage.xaml.cs

```csharp
using Microsoft.Maui.Controls.Shapes;
using ESP32.ViewModels;

namespace ESP32
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainPageViewModel();
            BindingContext = _viewModel;

            // Suscribirse a eventos de mensajes
            _viewModel.OnMessageReceived += AddMessageToChat;
        }

        // ========== EVENTO BOTÓN ENVIAR ==========

        private async void OnSendClicked(object sender, EventArgs e)
        {
            _viewModel.OnSendClicked(MessageEntry.Text, text => MessageEntry.Text = text);
        }

        // ========== AGREGAR MENSAJE AL CHAT ==========

        private void AddMessageToChat(string text, bool isUser)
        {
            // Crear burbuja de mensaje
            var frame = new Border
            {
                BackgroundColor = isUser ? Color.FromArgb("#007AFF") : Color.FromArgb("#E5E5EA"),
                Padding = new Thickness(12, 10),
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 280,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Content = new Label
                {
                    Text = text,
                    TextColor = isUser ? Colors.White : Colors.Black,
                    FontSize = 15
                }
            };
            
            MessagesContainer.Children.Add(frame);

            // Auto-scroll al último mensaje
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                await ChatScrollView.ScrollToAsync(MessagesContainer, ScrollToPosition.End, true);
            });
        }
    }
}
```

### 📝 Código: MainPageViewModel.cs

```csharp
using System.Net.Http.Json;
using ESP32.Models;

namespace ESP32.ViewModels
{
    public class MainPageViewModel
    {
        private readonly HttpClient _httpClient = new HttpClient();

        // ========== EVENTO DE MENSAJES ==========

        public event Action<string, bool>? OnMessageReceived;

        // ========== PROCESAR ENVÍO DE MENSAJE ==========

        public async void OnSendClicked(string messageText, Action<string> clearMessage)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            // 1️⃣ Mostrar mensaje del usuario
            OnMessageReceived?.Invoke(messageText, true);
            clearMessage(string.Empty);

            try
            {
                // 2️⃣ Enviar al MCP Client
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:5256/api/chat", 
                    new { message = messageText }
                );

                if (response.IsSuccessStatusCode)
                {
                    // 3️⃣ Procesar respuesta
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
```

### 🎨 Interfaz de Usuario

#### Layout

```
┌─────────────────────────────────┐
│   Contenedor de Mensajes        │
│  ┌───────────────┐              │
│  │ Mensaje Usuario│ →            │  (Azul, derecha)
│  └───────────────┘              │
│              ┌──────────────────┤
│              │ Mensaje del Bot  │  (Gris, izquierda)
│              └──────────────────┤
│  ┌───────────────┐              │
│  │ Otra pregunta │→             │
│  └───────────────┘              │
└─────────────────────────────────┘
│                                 │
│  [          MessageEntry       ]│  (Campo de entrada)
│  [Enviar]                       │  (Botón)
└─────────────────────────────────┘
```

#### Estilos de Mensaje

**Mensaje del Usuario:**
- Color: `#007AFF` (Azul)
- Texto: Blanco
- Posición: Derecha
- Ancho máximo: 280px
- Bordes redondeados: 16px

**Mensaje del Bot:**
- Color: `#E5E5EA` (Gris)
- Texto: Negro
- Posición: Izquierda
- Ancho máximo: 280px
- Bordes redondeados: 16px

### 🔄 Flujo de Interacción

1. **Usuario escribe mensaje** en `MessageEntry`
2. **Click en botón Enviar** → `OnSendClicked()`
3. **ViewModel muestra mensaje** → `OnMessageReceived?.Invoke(mensaje, true)`
4. **POST a MCP Client** → `http://localhost:5256/api/chat`
5. **Recibe respuesta** → `ChatResponse`
6. **Muestra respuesta** → `OnMessageReceived?.Invoke(respuesta, false)`
7. **Auto-scroll** al último mensaje

### 📡 API Call

```csharp
POST http://localhost:5256/api/chat
Content-Type: application/json

{
  "message": "Enciende la luz roja"
}
```

**Respuesta esperada:**
```json
{
  "action": "EncenderLuzRoja",
  "result": "Luz roja encendida",
  "message": "Luz roja encendida",
  "success": true
}
```

---

## 🔄 Flujo de Comunicación Completo

### Ejemplo: "Enciende la luz roja"

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1️⃣ Usuario en MAUI: "Enciende la luz roja"                          │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 2️⃣ MainPageViewModel.OnSendClicked()                                │
│    POST http://localhost:5256/api/chat                              │
│    Body: { "message": "Enciende la luz roja" }                      │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 3️⃣ ChatController.Post()                                            │
│    ChatService.ProcessMessageAsync()                                │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 4️⃣ ChatService:                                                     │
│    - ListarTools() desde MCP Server                                 │
│    - BuildToolDescriptions()                                        │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 5️⃣ POST http://localhost:1234/v1/chat/completions                  │
│    Mensaje del sistema + tools + pregunta del usuario               │
│    Modelo: meta-llama-3.1-8b-instruct                               │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 6️⃣ LM Studio interpreta:                                            │
│    INPUT: "Enciende la luz roja"                                    │
│    OUTPUT: "TOOL:EncenderLuzRoja"                                   │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 7️⃣ ChatService.ExtractToolName()                                    │
│    Busca "TOOL:" en respuesta                                       │
│    Extrae: "EncenderLuzRoja"                                        │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 8️⃣ McpClient.CallToolAsync("EncenderLuzRoja", {})                   │
│    Comunica con MCP Server vía Stdio                                │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 9️⃣ Tools.EncenderLuzRoja()                                          │
│    Enviar("encender_rojo")                                          │
│    POST http://127.0.0.1:1880/luz                                   │
│    Body: "encender_rojo" (text/plain)                               │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 🔟 Node-RED /luz endpoint:                                          │
│    Recibe payload "encender_rojo"                                   │
│    MQTT Publish:                                                    │
│      Topic: tarea7/luces                                            │
│      Message: encender_rojo                                         │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 1️⃣1️⃣ Mosquitto MQTT Broker:                                        │
│    test.mosquitto.org:1883                                          │
│    Distribuye mensaje a suscriptores                                │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 1️⃣2️⃣ ESP32 MQTT Subscriber:                                        │
│    Recibe: "encender_rojo" en topic tarea7/luces                   │
│    callback() activa encenderLed("rojo")                            │
└────────────────────┬────────────────────────────────────────────────┘

┌────────────────────▼────────────────────────────────────────────────┐
│ 1️⃣3️⃣ Hardware:                                                      │
│    - digitalWrite(LED_ROJO, HIGH)                                   │
│    - sonidoRojo()                                                   │
│    - mostrarColorPantalla("rojo")                                   │
│    ✅ LED rojo encendido                                             │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ 1️⃣4️⃣ Respuesta a MAUI:                                              │
│    ChatResponse:                                                    │
│    {                                                                │
│      "action": "EncenderLuzRoja",                                   │
│      "result": "Luz roja encendida",                                │
│      "message": "Luz roja encendida",                               │
│      "success": true                                                │
│    }                                                                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ 1️⃣5️⃣ MAUI muestra:                                                  │
│    [Bot]: "Luz roja encendida"                                      │
│    (Burbuja gris a la izquierda)                                    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Instrucciones de Instalación

### Requisitos Previos

- **Windows 10/11** con .NET 6 o superior
- **Visual Studio 2022** Community Edition
- **Arduino IDE** o PlatformIO
- **LM Studio** instalado
- **Node-RED** con Node.js
- **ESP32** con drivers USB UART

### Paso 1: Configurar ESP32

1. Conectar ESP32 a la computadora vía USB
2. Abrir Arduino IDE
3. Instalar librerías:
   - `WiFi.h` (incluida)
   - `PubSubClient.h` (Sketch → Include Library → Manage Libraries)
   - `Adafruit_GFX.h`
   - `Adafruit_SSD1306.h`
4. Configurar tarjeta:
   - Board: "ESP32 Dev Module"
   - Port: COM (correspondiente)
5. Cargar código C++ proporcionado
6. Verificar en Serial Monitor (115200 baud)

### Paso 2: Instalar y Ejecutar MCP Server

```bash
cd C:/Users/jamar/source/repos/ESP32/MCP_Server
dotnet restore
dotnet run
```

**Salida esperada:**
```
Listening for incoming client connections...
```

### Paso 3: Instalar y Ejecutar MCP Client

```bash
cd C:/Users/jamar/source/repos/ESP32/MCP_Client
dotnet restore
dotnet run --urls "http://localhost:5256"
```

**Salida esperada:**
```
Now listening on: http://localhost:5256
```

### Paso 4: Instalar y Ejecutar LM Studio

1. Descargar desde https://lmstudio.ai
2. Instalar modelo `meta-llama-3.1-8b-instruct`
3. Iniciar servidor:
   - Ir a "Local Server"
   - Seleccionar modelo
   - Click "Start Server"
4. Verificar: `http://localhost:1234/v1/chat/completions`

### Paso 5: Levantar contenedor de Node-RED

```bash
docker container run -it -p 1880:1880 --name node-red nodered/node-red
```

**Acceder a:** `http://localhost:1880`

**Crear flujo:**
1. Nodo HTTP Input (POST /luz)
2. Nodo MQTT Output (Topic: tarea7/luces)
3. Conectar y hacer Deploy

### Paso 6: Ejecutar Aplicación MAUI

```bash
cd C:/Users/jamar/source/repos/ESP32/MAUI_App
dotnet restore
dotnet run
```

### Verificación Final

1. ✅ ESP32 conectado a WiFi (`ESP32TEST`)
2. ✅ MCP Server corriendo en Stdio
3. ✅ MCP Client API respondiendo en `localhost:5256`
4. ✅ LM Studio activo en `localhost:1234`
5. ✅ Node-RED corriendo en `localhost:1880`
6. ✅ MAUI aplicación iniciada

---

## 🐛 Solución de Problemas

### ESP32 no se conecta a WiFi

**Síntomas:**
- Serial Monitor muestra puntos continuos
- "WiFi conectado" nunca aparece

**Soluciones:**
1. Verificar credenciales WiFi:
   ```cpp
   const char* SSID = "ESP32TEST";
   const char* PASSWORD = "12345678";
   ```
2. Revisar que el router permita conexión 2.4GHz
3. Reiniciar ESP32: `digitalWrite(RST, LOW);`

### MQTT no conecta

**Síntomas:**
- "Error MQTT, rc=..." en Serial Monitor
- No recibe comandos

**Soluciones:**
1. Verificar conexión a internet en ESP32
2. Probar con broker público:
   ```cpp
   const char* MQTT_SERVER = "test.mosquitto.org";
   ```
3. Verificar firewall permite puerto 1883

### LM Studio no responde

**Síntomas:**
- Error: "Error conectando con LM Studio"
- Timeout en ChatService

**Soluciones:**
1. Verificar LM Studio está corriendo: `localhost:1234`
2. Confirmar modelo cargado (`meta-llama-3.1-8b-instruct`)
3. Revisar logs en interfaz de LM Studio
4. Reiniciar LM Studio

### Node-RED no publica MQTT

**Síntomas:**
- Luces no se encienden
- No hay error en Node-RED

**Soluciones:**
1. Verificar conexión MQTT en propiedades del nodo
2. Revisar que broker esté activo
3. Usar MQTT Explorer para testear publicación manual
4. Comprobar topic correcto: `tarea7/luces`

### MCP Client no conecta con MCP Server

**Síntomas:**
- Error: "Error procesando solicitud"
- MCP Client no inicia

**Soluciones:**
1. Verificar ruta del proyecto MCP Server:
   ```csharp
   "C:/Users/jamar/source/repos/ESP32/MCP_Server/MCP_Server.csproj"
   ```
2. Confirmar MCP Server compila sin errores
3. Revisar puertos: Stdio (no hay puerto específico)

### MAUI no conecta con API

**Síntomas:**
- "Error al conectar: Host not found"
- Aplicación no envía mensajes

**Soluciones:**
1. Verificar URL correcta:
   ```csharp
   "http://localhost:5256/api/chat"
   ```
2. En emulador Android, usar `10.0.2.2` en lugar de `localhost`
3. Desactivar firewall temporalmente
4. Verificar API está corriendo

---


