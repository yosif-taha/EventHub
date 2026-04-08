using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Common.Dtos.Events
{
    public class EventDto
    {
        public Guid Id { get; set; } 
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
        public string Status { get; set; } = string.Empty; 
        public bool PaymentRequired { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
