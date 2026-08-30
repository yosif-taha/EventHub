namespace EventHub.MVC.Services.Api;

public sealed class ApiResponseEnvelope<T>
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public int? ErrorCode { get; init; }
    public T? Data { get; init; }
}
