using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace EventHub.Application.Contracts
{
    public interface IUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel, CancellationToken cancellationToken);
    }
}
