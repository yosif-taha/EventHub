

namespace EventHub.Application.Common.Responses
{
    public record RequestResult<TResult>
    {
        public TResult? Data { get; init; }
        public bool IsSuccess { get; init; }
        public ErrorCode ErrorCode { get; init; }
        public string? Message { get; init; } // لشرح سبب الخطأ بالتفصيل

        private RequestResult(TResult? data, bool isSuccess, ErrorCode errorCode, string? message = null)
        {
            Data = data;
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            Message = message;
        }

        // النجاح دائماً يحتاج بيانات
        public static RequestResult<TResult> Success(TResult data) => new(data, true, ErrorCode.None);

        // الـ Overload الأول: فشل مع كود الخطأ فقط
        public static RequestResult<TResult> Failure(ErrorCode errorCode) => new(default, false, errorCode);

        // الـ Overload الثاني: فشل مع كود الخطأ ورسالة مخصصة (وهذا ما نحتاجه في الـ Validation)
        public static RequestResult<TResult> Failure(ErrorCode errorCode, string message) => new(default, false, errorCode, message);
    }
    //public record RequestResult<TResult>
    //{
    //    public TResult? Data { get; init; }
    //    public bool IsSuccess { get; init; }
    //    public ErrorCode ErrorCode { get; init; }

    //    private RequestResult(TResult? data, bool isSuccess, ErrorCode errorCode)
    //    {
    //        Data = data;
    //        IsSuccess = isSuccess;
    //        ErrorCode = errorCode;
    //    }
    //    public static RequestResult<TResult> Success(TResult data) => new(data, true, ErrorCode.None);
    //    public static RequestResult<TResult> Failure(ErrorCode errorCode) => new(default, false, errorCode);
    //}
}
