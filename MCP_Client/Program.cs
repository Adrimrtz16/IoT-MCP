using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

var app = builder.Build();

app.MapControllers();

app.Run();