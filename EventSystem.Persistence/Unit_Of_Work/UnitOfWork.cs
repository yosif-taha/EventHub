using EventHub.Application.Contracts;
using EventHub.Application.Common.Responses;
using EventHub.Persistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EventHub.Persistence.Unit_Of_Work
{
    public class UnitOfWork(EventDbContext _context) : IUnitOfWork
    {
        private IDbContextTransaction? _transaction;
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(action, IsolationLevel.ReadCommitted, cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            var isOuterTransaction = _transaction is null;
            if (isOuterTransaction)
                _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await action();
                if (isOuterTransaction)
                {
                    if (result is ITransactionResult transactionResult && !transactionResult.IsSuccess)
                    {
                        await _transaction!.RollbackAsync(cancellationToken);
                        _context.ChangeTracker.Clear();
                        return result;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await _transaction!.CommitAsync(cancellationToken);
                }
                return result;
            }
            catch (Exception)
            {
                if (isOuterTransaction && _transaction is not null)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                }
                throw;
            }
            finally
            {
                if (isOuterTransaction && _transaction is not null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
    }
}
