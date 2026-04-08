using EventHub.Application.Common.Responses;

namespace EventHub.WebAPI.Presentation.ViewModels.Respponse
{
    public class FailedResponseViewModel : ResponseViewModel
    {
        public ErrorCode ErrorCode { get; set; }

        public FailedResponseViewModel(ErrorCode errorCode, string message) 
        {
            Message = message;
            IsSuccess = false;
            ErrorCode = errorCode;
        }
    }
}
