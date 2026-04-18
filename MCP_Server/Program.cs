using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

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

    static async Task Enviar(string payload)
    {
        var content = new StringContent(payload, Encoding.UTF8);
        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        await client.PostAsync("http://127.0.0.1:1880/luz", content);
    }
}