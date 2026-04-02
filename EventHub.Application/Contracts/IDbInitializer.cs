using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Contracts
{
    public interface IDbInitializer
    {
        Task IntiliazeAsync();
    }
}
