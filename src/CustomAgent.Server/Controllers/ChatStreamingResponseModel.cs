namespace CustomAgent.Server.Controllers
{
    public class ChatStreamingResponseModel
    {
        public Guid Reference { get; set; }
        public string Chunk { get; set; } = string.Empty;
        public bool LastChunk { get; set; } = false;
        public string? Error { get; set; }
    }
}
