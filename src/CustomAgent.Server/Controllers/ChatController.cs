using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using System.ClientModel;
using System.Text.Json;

namespace CustomAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AIAgent _agent;
    public ChatController([FromKeyedServices("azure")] AIAgent agent)
    {
        _agent = agent;
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

        var messages = model.ToChatMessages();
        try
        {
            //var response = await chatService.GetChatMessageContentAsync(history, _promptExecutionSettings, _kernel);
            //var payload = JsonSerializer.Serialize(new ChatStreamingResponseModel
            //{
            //    Reference = reference,
            //    Chunk = response.Content!
            //});
            //await Response.WriteAsync($"data: {payload}\n\n", timeoutCts.Token);
            //await Response.Body.FlushAsync(timeoutCts.Token);

            //var endMessage = JsonSerializer.Serialize(new ChatStreamingResponseModel
            //{
            //    Reference = reference,
            //    LastChunk = true
            //});
            //await Response.WriteAsync(
            //    $"data: {endMessage}\n\n",
            //    cancellationToken);

            //await Response.Body.FlushAsync(
            //    cancellationToken);

            await foreach (var update in _agent.RunStreamingAsync(messages, cancellationToken: timeoutCts.Token))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    var payload = JsonSerializer.Serialize(new ChatStreamingResponseModel
                    {
                        Reference = reference,
                        Chunk = update.Text
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

