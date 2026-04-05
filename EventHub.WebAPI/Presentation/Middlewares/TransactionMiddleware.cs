using EventHub.Persistence.Data.Contexts;
using System;

namespace EventHub.WebAPI.Presentation.Middlewares
{
    public class TransactionMiddleware(EventDbContext context) : IMiddleware
    {
        private readonly EventDbContext _context = context;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                await next(context);
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await next(context);
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
