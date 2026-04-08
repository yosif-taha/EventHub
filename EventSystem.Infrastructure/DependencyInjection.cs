using EventHub.Application.Contracts;
using EventHub.Infrastructure.Account;
using EventHub.Infrastructure.Auth;
using EventHub.Infrastructure.Common;
using EventHub.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Auth & JWT
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<IAccountService, AccountService>();

            // Email
            services.Configure<EmailSettings>(configuration.GetSection(nameof(EmailSettings)));
            services.AddScoped<IEmailService, EmailService>();

            // User Context
            services.AddScoped<IUserContext, UserContext>();

            // Pagination
            services.AddScoped<IDbExecutor, DbExecutor>();

            // JWT Options Validation
            services.AddOptions<JwtOptions>()
                .BindConfiguration("Jwt")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
