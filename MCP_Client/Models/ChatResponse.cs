namespace MCP_Client.Models
{
    public class ChatResponse
    {
        public string? Action { get; set; }
        public string? Result { get; set; }
        public string Message { get; set; } = "";
        public bool Success { get; set; }
    }
}
