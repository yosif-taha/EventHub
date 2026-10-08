using System.Net;
using System.Text;

namespace EventHub.Tests.Support;

// Records immutable snapshots because production clients dispose their request messages.
internal sealed class RecordingHttpHandler(Func<int, HttpResponseMessage> response) : HttpMessageHandler
{
    public List<(HttpMethod Method, string Uri, string? Body, string? Authorization)> Requests { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add((request.Method, request.RequestUri!.ToString(),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Headers.Authorization?.ToString()));
        return response(Requests.Count - 1);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
