using System.Net;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFunction;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFunction)
    {
        _responseFunction = responseFunction;
    }
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseHandler = (_, _) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

    public void SetResponse(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        _responseHandler = responseHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _responseHandler(request, cancellationToken);
    }
}