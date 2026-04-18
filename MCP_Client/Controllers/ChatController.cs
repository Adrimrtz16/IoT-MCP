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

    [HttpGet("tools")]
    public async Task<IActionResult> GetTools()
    {
        var tools = await _client.ListToolsAsync();
        return Ok(tools);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest body)
    {
        var response = await _chatService.ProcessMessageAsync(body.message);
        return Ok(response);
    }
}