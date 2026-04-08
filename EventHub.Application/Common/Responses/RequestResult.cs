

namespace EventHub.Application.Common.Responses
{
    public record RequestResult<TResult>
    {
        public TResult? Data { get; init; }
        public bool IsSuccess { get; init; }
        public ErrorCode ErrorCode { get; init; }

        private RequestResult(TResult? data, bool isSuccess, ErrorCode errorCode)
        {
            Data = data;
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
        }
        public static RequestResult<TResult> Success(TResult data) => new(data, true, ErrorCode.None);
        public static RequestResult<TResult> Failure(ErrorCode errorCode) => new(default, false, errorCode);
    }
}
