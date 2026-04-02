using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    [Owned]
    public class RefreshTokens
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; } = DateTime.UtcNow;
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public bool IsActive => RevokedOn == null && !IsExpired;

    }
}
