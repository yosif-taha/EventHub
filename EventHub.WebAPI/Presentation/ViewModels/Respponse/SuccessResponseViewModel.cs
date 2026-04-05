using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.WebAPI.Presentation.ViewModels.Respponse
{
    public class SuccessResponseViewModel: ResponseViewModel
    {
        public SuccessResponseViewModel(string message) 
        {
            IsSuccess = true;
            Message = message;
        }
        public SuccessResponseViewModel() 
        {
            IsSuccess = true;
        }
    }
}
