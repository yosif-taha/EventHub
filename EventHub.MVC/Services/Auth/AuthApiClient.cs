using EventHub.MVC.Models.Auth;
using EventHub.MVC.Services.Api;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventHub.MVC.Services.Auth;

public sealed class AuthApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthApiClient> logger) : IAuthApiClient
{
    public const string HttpClientName = "EventHubAuthenticationApi";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiCallResult<AuthResponseDto>> LoginAsync(LoginViewModel request, CancellationToken cancellationToken) =>
        PostAsync<LoginApiRequest, AuthResponseDto>(
            "api/Auth/Login",
            new LoginApiRequest(request.Email, request.Password),
            cancellationToken);

    public Task<ApiCallResult<Guid>> RegisterAsync(RegisterViewModel request, CancellationToken cancellationToken) =>
        PostAsync<RegisterApiRequest, Guid>(
            "api/Auth/Register",
            new RegisterApiRequest(request.FullName, request.Email, request.Password, request.PhoneNumber),
            cancellationToken);

    public Task<ApiCallResult<bool>> ConfirmEmailAsync(Guid userId, string code, CancellationToken cancellationToken) =>
        PostOperationAsync(
            "api/Auth/ConfirmEmail",
            new ConfirmEmailApiRequest(userId.ToString(), code),
            cancellationToken);

    public Task<ApiCallResult<bool>> ResetPasswordAsync(ResetPasswordViewModel request, CancellationToken cancellationToken) =>
        PostOperationAsync(
            "api/Auth/ResetPassword",
            new ResetPasswordApiRequest(request.Email, request.Code, request.NewPassword),
            cancellationToken);

    private async Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName)
                .PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            ApiResponseEnvelope<TResponse>? envelope = null;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<TResponse>>(JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                logger.LogWarning("The backend API returned an unreadable response for {Endpoint} with status {StatusCode}.", endpoint, response.StatusCode);
            }

            if (response.IsSuccessStatusCode && envelope?.IsSuccess == true && envelope.Data is not null)
                return ApiCallResult<TResponse>.Success(envelope.Data);

            var message = envelope?.Message ?? GetSafeFailureMessage(response.StatusCode);
            return ApiCallResult<TResponse>.Failure(message, GetFailureKind(response.StatusCode, envelope?.ErrorCode), envelope?.ErrorCode);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("The backend API could not be reached for {Endpoint}.", endpoint);
            return ApiCallResult<TResponse>.Failure(
                "The service is currently unavailable. Please try again shortly.",
                ApiFailureKind.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The backend API request timed out for {Endpoint}.", endpoint);
            return ApiCallResult<TResponse>.Failure(
                "The service is taking too long to respond. Please try again.",
                ApiFailureKind.Unavailable);
        }
    }

    private async Task<ApiCallResult<bool>> PostOperationAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName)
                .PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            ApiResponseEnvelope<JsonElement>? envelope = null;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<JsonElement>>(JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                logger.LogWarning("The backend API returned an unreadable response for {Endpoint} with status {StatusCode}.", endpoint, response.StatusCode);
            }

            if (response.IsSuccessStatusCode && envelope?.IsSuccess == true)
                return ApiCallResult<bool>.Success(true);

            var message = envelope?.Message ?? GetSafeFailureMessage(response.StatusCode);
            return ApiCallResult<bool>.Failure(message, GetFailureKind(response.StatusCode, envelope?.ErrorCode), envelope?.ErrorCode);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("The backend API could not be reached for {Endpoint}.", endpoint);
            return ApiCallResult<bool>.Failure(
                "The service is currently unavailable. Please try again shortly.",
                ApiFailureKind.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The backend API request timed out for {Endpoint}.", endpoint);
            return ApiCallResult<bool>.Failure(
                "The service is taking too long to respond. Please try again.",
                ApiFailureKind.Unavailable);
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

    private sealed record LoginApiRequest(string Email, string Password);

    private sealed record RegisterApiRequest(string FullName, string Email, string Password, string? PhoneNumber);
    private sealed record ConfirmEmailApiRequest(string UserId, string Code);
    private sealed record ResetPasswordApiRequest(string Email, string Code, string NewPassword);
}
