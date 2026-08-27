using EventHub.Application.Contracts;
using EventHub.Persistence.Data.Contexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventHub.Persistence.Unit_Of_Work
{
    public class UnitOfWork(EventDbContext _context) : IUnitOfWork
    {
        private IDbContextTransaction? _transaction;
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            var isOuterTransaction = _transaction is null;
            if (isOuterTransaction)
                _transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var result = await action();
                if (isOuterTransaction)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    await _transaction!.CommitAsync(cancellationToken);
                }
                return result;
            }
            catch (Exception)
            {
                if (isOuterTransaction && _transaction is not null)
                    await _transaction.RollbackAsync(cancellationToken);
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
