using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventHub.MVC.Services.Api;

public sealed class BackendApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<BackendApiClient> logger) : IBackendApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiCallResult<TResponse>> GetAsync<TResponse>(string clientName, string endpoint, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(clientName, new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken);

    public Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string clientName,
        string endpoint,
        TRequest? request,
        CancellationToken cancellationToken)
    {
        return SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Post, clientName, endpoint, request, cancellationToken);
    }

    public Task<ApiCallResult<TResponse>> PutAsync<TRequest, TResponse>(
        string clientName,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken) =>
        SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Put, clientName, endpoint, request, cancellationToken);

    public Task<ApiCallResult<TResponse>> PatchAsync<TRequest, TResponse>(
        string clientName,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken) =>
        SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Patch, clientName, endpoint, request, cancellationToken);

    public async Task<ApiCallResult<bool>> DeleteAsync(string clientName, string endpoint, CancellationToken cancellationToken)
    {
        var result = await SendAsync<JsonElement>(clientName, new HttpRequestMessage(HttpMethod.Delete, endpoint), cancellationToken);
        return result.IsSuccess
            ? ApiCallResult<bool>.Success(true)
            : ApiCallResult<bool>.Failure(result.Message, result.FailureKind, result.ErrorCode);
    }

    private Task<ApiCallResult<TResponse>> SendWithBodyAsync<TRequest, TResponse>(
        HttpMethod method,
        string clientName,
        string endpoint,
        TRequest? request,
        CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(method, endpoint);
        if (request is not null)
            message.Content = JsonContent.Create(request, options: JsonOptions);

        return SendAsync<TResponse>(clientName, message, cancellationToken);
    }

    private async Task<ApiCallResult<TResponse>> SendAsync<TResponse>(
        string clientName,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using (request)
            using (var response = await httpClientFactory.CreateClient(clientName).SendAsync(request, cancellationToken))
            {
                ApiResponseEnvelope<TResponse>? envelope = null;
                try
                {
                    envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<TResponse>>(JsonOptions, cancellationToken);
                }
                catch (JsonException)
                {
                    logger.LogWarning("The backend API returned an unreadable response for {Endpoint} with status {StatusCode}.", request.RequestUri, response.StatusCode);
                }

                if (response.IsSuccessStatusCode && envelope?.IsSuccess == true)
                    return ApiCallResult<TResponse>.Success(envelope.Data!);

                var message = envelope?.Message ?? GetSafeFailureMessage(response.StatusCode);
                return ApiCallResult<TResponse>.Failure(message, GetFailureKind(response.StatusCode, envelope?.ErrorCode), envelope?.ErrorCode);
            }
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("The backend API could not be reached for {Endpoint}.", request.RequestUri);
            return ApiCallResult<TResponse>.Failure("The service is currently unavailable. Please try again shortly.", ApiFailureKind.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The backend API request timed out for {Endpoint}.", request.RequestUri);
            return ApiCallResult<TResponse>.Failure("The service is taking too long to respond. Please try again.", ApiFailureKind.Unavailable);
        }
    }

    private static ApiFailureKind GetFailureKind(HttpStatusCode statusCode, int? errorCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => ApiFailureKind.Unauthorized,
            HttpStatusCode.Forbidden => ApiFailureKind.Forbidden,
            HttpStatusCode.NotFound => ApiFailureKind.NotFound,
            HttpStatusCode.InternalServerError => ApiFailureKind.Server,
            _ when errorCode == 101 => ApiFailureKind.Validation,
            _ when errorCode == 104 => ApiFailureKind.Unauthorized,
            _ when errorCode == 105 => ApiFailureKind.Forbidden,
            _ when errorCode is 300 or 400 => ApiFailureKind.NotFound,
            _ => ApiFailureKind.Server
        };

    private static string GetSafeFailureMessage(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session is invalid or has expired. Please sign in again.",
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested service could not be found.",
            _ => "We could not complete your request. Please try again."
        };
}
