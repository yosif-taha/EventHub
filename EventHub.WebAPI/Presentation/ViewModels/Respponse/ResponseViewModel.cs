
namespace EventHub.WebAPI.Presentation.ViewModels.Respponse
{
    public abstract class ResponseViewModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}
