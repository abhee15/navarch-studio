namespace ApiGateway.Services;

public interface IHttpClientService
{
    Task<HttpResponseMessage> GetAsync(string service, string endpoint, CancellationToken cancellationToken);
    Task<HttpResponseMessage> PostAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken);
}





