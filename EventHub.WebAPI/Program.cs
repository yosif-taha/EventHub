using EventHub.Application;
using EventHub.Infrastructure;
using EventHub.Persistence;
using EventHub.WebAPI.Presentation.Extensions;

namespace EventHub.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration)
            .AddPersistence(builder.Configuration)
            .AddPresentation(builder.Configuration);

            var app = builder.Build();

           app.UseCustomMiddlewares();
           await app.UseDbInitializer();

            app.Run();
        }
    }
}
