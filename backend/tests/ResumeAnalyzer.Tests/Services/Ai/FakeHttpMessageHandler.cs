using System.Net;
using System.Text;

namespace ResumeAnalyzer.Tests.Services.Ai;

/// <summary>
/// Returns queued responses in order and records each request, so tests never call the real API.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public FakeHttpMessageHandler Enqueue(HttpStatusCode status, string body, Action<HttpResponseMessage>? configure = null)
    {
        _responses.Enqueue(() =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            configure?.Invoke(response);
            return response;
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri, request.Headers.ToDictionary(
            h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase), body));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No fake response queued for this request.");
        }

        return _responses.Dequeue()();
    }

    public sealed record RecordedRequest(
        HttpMethod Method,
        Uri? Uri,
        IReadOnlyDictionary<string, string> Headers,
        string Body);
}
