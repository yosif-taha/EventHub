using EventHub.Domin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    public class EventCategory : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public List<Event> Events { get; set; } = []; // Navigation property
    }
}
