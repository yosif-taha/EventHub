using EventHub.Application.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
