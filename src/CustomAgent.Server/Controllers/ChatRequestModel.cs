using Microsoft.Extensions.AI;

namespace CustomAgent.Server.Controllers
{
    public class ChatRequestModel
    {
        public ChatRequestModel()
        {

        }
        public IList<ChatMessageModel> ChatHistory { get; set; } = [];
        public string SystemPrompt { get; set; } = string.Empty;
        public string MessagePrompt { get; set; } = string.Empty;

        public List<ChatMessage> ToChatMessages()
        {
            return
                [
                    new ChatMessage(ChatRole.System, SystemPrompt),
                    .. ChatHistory.Select(x => new ChatMessage(x.Role == "User" ? ChatRole.User : ChatRole.Assistant, x.Content)),
                    new ChatMessage(ChatRole.User, MessagePrompt)
                ];
        }
    }

    public class ChatMessageModel
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
