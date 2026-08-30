using System.Net.Http;

namespace EventHub.MVC.Services.Api;

public interface IBackendApiClient
{
    Task<ApiCallResult<TResponse>> GetAsync<TResponse>(string clientName, string endpoint, CancellationToken cancellationToken);
    Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(string clientName, string endpoint, TRequest? request, CancellationToken cancellationToken);
    Task<ApiCallResult<TResponse>> PutAsync<TRequest, TResponse>(string clientName, string endpoint, TRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<TResponse>> PatchAsync<TRequest, TResponse>(string clientName, string endpoint, TRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<bool>> DeleteAsync(string clientName, string endpoint, CancellationToken cancellationToken);
}
