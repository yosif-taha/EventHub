using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.WebAPI.Presentation.ViewModels.Respponse
{
    public abstract class ResponseViewModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}
