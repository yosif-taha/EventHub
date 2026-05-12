using EventHub.Application.Contracts;
using EventHub.Persistence.Data.Contexts;
using EventHub.Persistence.DataSeeding;
using EventHub.Persistence.Reposetories;
using EventHub.Persistence.Unit_Of_Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EventDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
          
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IDbInitializer, DbInitializer>();



            return services;
        }
    }
}
