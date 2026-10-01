using System.Net;

namespace HowsMyMoney.Tests.TestHelpers;

/// <summary>
/// HttpMessageHandler de prueba: siempre devuelve el mismo contenido, sin tocar la red.
/// Se inyecta vía HttpClient a los servicios que aceptan un HttpClient opcional.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    public string? LastRequestUri { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri?.ToString();

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent)
        };

        return Task.FromResult(response);
    }

    public static HttpClient CreateClient(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(new FakeHttpMessageHandler(responseContent, statusCode));
}
