using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CustomAgent.Server.Controllers
{
    public class ChatRequestModel
    {
        public ChatRequestModel()
        {

        }
        public IList<ChatMessage> ChatHistory { get; set; } = [];
        public string SystemPrompt { get; set; } = string.Empty;
        public string MessagePrompt { get; set; } = string.Empty;

        public ChatHistory ToChatHistory()
        {
            var history = new ChatHistory(
                [
                    new ChatMessageContent(AuthorRole.System, SystemPrompt),
                    .. ChatHistory.Select(x => new ChatMessageContent(x.Role == "User" ? AuthorRole.User : AuthorRole.Assistant, x.Content)),
                    new ChatMessageContent(AuthorRole.User, MessagePrompt)
                ]);
            return history;
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
