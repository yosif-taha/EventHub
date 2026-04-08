
namespace EventHub.WebAPI.Presentation.ViewModels.Respponse
{
    public class SuccessResponseViewModelT<T> : ResponseViewModel
    {
        public T Data { get; set; }
        public SuccessResponseViewModelT(T data , string message) 
        {
            IsSuccess = true;
            Data = data;
            Message = message;
        }
        public SuccessResponseViewModelT(T data ) 
        {
            IsSuccess = true;
            Data = data;
        }
    }
}
