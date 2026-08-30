namespace EventHub.MVC.Services.Api;

public sealed class ApiCallResult<T>
{
    private ApiCallResult(bool isSuccess, T? data, string message, ApiFailureKind failureKind, int? errorCode)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
        FailureKind = failureKind;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public T? Data { get; }
    public string Message { get; }
    public ApiFailureKind FailureKind { get; }
    public int? ErrorCode { get; }

    public static ApiCallResult<T> Success(T data) => new(true, data, string.Empty, ApiFailureKind.None, null);

    public static ApiCallResult<T> Failure(string message, ApiFailureKind failureKind, int? errorCode = null) =>
        new(false, default, message, failureKind, errorCode);
}
