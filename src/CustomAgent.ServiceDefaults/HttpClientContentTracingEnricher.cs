using System.Diagnostics;
using System.Text;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Tags outgoing HttpClient spans (e.g. Semantic Kernel calling Azure OpenAI) with http.request.content /
// http.response.content, mirroring what HttpContentTracingMiddleware does for incoming ASP.NET Core requests.
// Wired into AddHttpClientInstrumentation's Enrich hooks since there's no middleware pipeline to hang a
// pass-through stream off of on the outbound side.
internal static class HttpClientContentTracingEnricher
{
    // Set once at startup (see Extensions.ConfigureOpenTelemetry) so streaming responses can force an export
    // as soon as they finish, instead of waiting for Aspire's dashboard-tuned batch schedule (OTEL_BSP_SCHEDULE_DELAY,
    // 1s by default) to tick. Without this, a span that's already been swept into a batch before the stream
    // finishes would export without the response content tag, with no way to add it after the fact.
    public static TracerProvider? TracerProvider { get; set; }

    public static void EnrichWithRequest(Activity activity, HttpRequestMessage request)
    {
        var content = request.Content;
        if (content is null || !HttpContentTagging.IsCapturableMediaType(content.Headers.ContentType?.MediaType))
        {
            return;
        }

        // Request bodies going out are always already fully buffered in memory by the caller (JSON payloads,
        // not live streams), so reading them here is cheap and never steals bytes from the actual send.
        try
        {
            var text = content.ReadAsStringAsync().GetAwaiter().GetResult();
            activity.SetTag("http.request.content", HttpContentTagging.Truncate(text));
        }
        catch
        {
            // Best-effort enrichment only; never let this break the real call.
        }
    }

    public static void EnrichWithResponse(Activity activity, HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!HttpContentTagging.IsCapturableMediaType(mediaType))
        {
            return;
        }

        try
        {
            if (mediaType?.Contains("event-stream", StringComparison.OrdinalIgnoreCase) is true)
            {
                // The body hasn't been read yet at this point — swap in a stream that mirrors every chunk into
                // a capture buffer as the real caller (e.g. Semantic Kernel) reads it, so live streaming is
                // untouched, and tag the activity once the stream is exhausted.
                var originalStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                var teeStream = new ResponseTeeStream(originalStream, activity);

                var newContent = new StreamContent(teeStream);
                foreach (var header in response.Content.Headers)
                {
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                response.Content = newContent;
            }
            else
            {
                var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                activity.SetTag("http.response.content", HttpContentTagging.Truncate(text));
            }
        }
        catch
        {
            // Best-effort enrichment only; never let this break the real call.
        }
    }
}

// Mirrors bytes read from a streaming response into a capped buffer, tagging the activity once the stream is
// exhausted. Reads are passed through untouched so consumers still see chunks the instant they arrive.
internal sealed class ResponseTeeStream(Stream inner, Activity activity) : Stream
{
    private readonly MemoryStream _capture = new();
    private bool _tagged;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);
        OnBytesRead(buffer.Span[..read]);
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    private void OnBytesRead(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            TagCapturedContent();
            return;
        }

        if (_capture.Length < HttpContentTagging.MaxCapturedBytes)
        {
            var toCapture = (int)Math.Min(data.Length, HttpContentTagging.MaxCapturedBytes - _capture.Length);
            _capture.Write(data[..toCapture]);
        }
    }

    private void TagCapturedContent()
    {
        if (_tagged)
        {
            return;
        }

        _tagged = true;
        var text = HttpContentTagging.Truncate(Encoding.UTF8.GetString(_capture.ToArray()));
        activity.SetTag("http.response.content", text);

        // The span already stopped before the stream finished, so it may already be sitting in a batch queued
        // for export. Force the export now rather than leaving it to the next scheduled tick, which can land
        // before this tag was ever set.
        var tracerProvider = HttpClientContentTracingEnricher.TracerProvider;
        if (tracerProvider is not null)
        {
            _ = Task.Run(() => tracerProvider.ForceFlush(2000));
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TagCapturedContent();
            _capture.Dispose();
            inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
