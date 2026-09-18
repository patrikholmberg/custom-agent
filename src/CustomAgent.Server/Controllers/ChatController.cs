using System.ClientModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CustomAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly PromptExecutionSettings _promptExecutionSettings;
    public ChatController(Kernel kernel, PromptExecutionSettings promptExecutionSettings)
    {
        _kernel = kernel;
        _promptExecutionSettings = promptExecutionSettings;
    }
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequestModel model, CancellationToken cancellationToken)
    {
        var reference = Guid.NewGuid();
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(120));

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        var history = model.ToChatHistory();
        try
        {
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(history, _promptExecutionSettings, _kernel, timeoutCts.Token))
            {
                if (chunk.Content is not null)
                {
                    var payload = JsonSerializer.Serialize(new ChatStreamingResponseModel
                    {
                        Reference = reference,
                        Chunk = chunk.Content
                    });
                    await Response.WriteAsync($"data: {payload}\n\n", timeoutCts.Token);
                    await Response.Body.FlushAsync(timeoutCts.Token);
                }
            }
            var endMessage = JsonSerializer.Serialize(new ChatStreamingResponseModel
            {
                Reference = reference,
                LastChunk = true
            });
            await Response.WriteAsync(
                $"data: {endMessage}\n\n",
                cancellationToken);

            await Response.Body.FlushAsync(
                cancellationToken);

        }
        catch (OperationCanceledException ex)
        {
            var endMessage = JsonSerializer.Serialize(new ChatStreamingResponseModel
            {
                Reference = reference,
                LastChunk = true,
                Error = ex.Message
            });
            await Response.WriteAsync(
                $"data: {endMessage}\n\n",
                cancellationToken);

            await Response.Body.FlushAsync(
                cancellationToken);
        }
        catch (ClientResultException ex)
        {
            var endMessage = JsonSerializer.Serialize(new ChatStreamingResponseModel
            {
                Reference = reference,
                LastChunk = true,
                Error = ex.Message
            });
            await Response.WriteAsync(
                $"data: {endMessage}\n\n",
                cancellationToken);

            await Response.Body.FlushAsync(
                cancellationToken);
        }
    }
}

