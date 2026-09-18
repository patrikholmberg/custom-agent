using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

// Captures request/response bodies onto the current Activity as http.request.content / http.response.content,
// since the stock ASP.NET Core OTel instrumentation only records headers/status, never body content.
public static class HttpContentTracingMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpContentTracing(this WebApplication app)
    {
        // Body content can contain secrets (API keys, chat content) and isn't cheap to buffer, so this only
        // ever runs in Development where the Aspire dashboard is the consumer.
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        return app.UseMiddleware<HttpContentTracingMiddleware>();
    }
}

internal sealed class HttpContentTracingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity is null || !context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        await CaptureRequestContentAsync(context, activity);

        var originalBody = context.Response.Body;
        await using var captureStream = new CapturingStream(originalBody, HttpContentTagging.MaxCapturedBytes);
        context.Response.Body = captureStream;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;

            if (HttpContentTagging.IsCapturableMediaType(context.Response.ContentType))
            {
                activity.SetTag("http.response.content", captureStream.GetCapturedText());
            }
        }
    }

    private static async Task CaptureRequestContentAsync(HttpContext context, Activity activity)
    {
        var request = context.Request;
        if (request.ContentLength is null or 0 || !HttpContentTagging.IsCapturableMediaType(request.ContentType))
        {
            return;
        }

        request.EnableBuffering();

        var length = (int)Math.Min(request.ContentLength.Value, HttpContentTagging.MaxCapturedBytes);
        var buffer = new byte[length];
        var read = await request.Body.ReadAtLeastAsync(buffer.AsMemory(0, length), length, throwOnEndOfStream: false, context.RequestAborted);
        request.Body.Position = 0;

        var content = Encoding.UTF8.GetString(buffer, 0, read);
        if (request.ContentLength > HttpContentTagging.MaxCapturedBytes)
        {
            content += "...[truncated]";
        }

        activity.SetTag("http.request.content", content);
    }
}

// Shared by the inbound (ASP.NET Core) and outbound (HttpClient) content-capture paths so both use the same
// size cap and the same notion of "text-ish, worth capturing" content.
internal static class HttpContentTagging
{
    public const int MaxCapturedBytes = 8 * 1024;

    public static bool IsCapturableMediaType(string? contentType) =>
        contentType is not null &&
        (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
         contentType.Contains("text/", StringComparison.OrdinalIgnoreCase));

    public static string Truncate(string text) =>
        text.Length > MaxCapturedBytes ? $"{text[..MaxCapturedBytes]}...[truncated]" : text;
}

// Mirrors bytes written to the response into a capped in-memory buffer while still passing every write straight
// through to the real response stream, so SSE/streaming responses keep flushing live instead of buffering fully.
internal sealed class CapturingStream(Stream inner, int maxCaptureBytes) : Stream
{
    private readonly MemoryStream _capture = new();

    public string GetCapturedText() => Encoding.UTF8.GetString(_capture.ToArray());

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_capture.Length < maxCaptureBytes)
        {
            var toCapture = (int)Math.Min(buffer.Length, maxCaptureBytes - _capture.Length);
            await _capture.WriteAsync(buffer[..toCapture], cancellationToken);
        }

        await inner.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override void Flush() => inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _capture.Dispose();
        }

        base.Dispose(disposing);
    }
}
