using EventHub.Application.Contracts;
using System.Data;

namespace EventHub.Tests.Support;

internal sealed class FailingUnitOfWork(Exception error) : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken) => Task.FromException<T>(error);
    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel, CancellationToken cancellationToken) => Task.FromException<T>(error);
}
