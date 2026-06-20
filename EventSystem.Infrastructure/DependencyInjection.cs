using EventHub.Application.Contracts;
using EventHub.Infrastructure.Account;
using EventHub.Infrastructure.Auth;
using EventHub.Infrastructure.Common;
using EventHub.Infrastructure.Email;
using EventHub.Infrastructure.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

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

            // Payment
            services.Configure<PaymobSettings>(configuration.GetSection(nameof(PaymobSettings)));
            services.AddHttpClient<IPaymobService, PaymobService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<PaymobSettings>>().Value;

                client.BaseAddress = new Uri(settings.BaseUrl);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            // JWT Options Validation
            services.AddOptions<JwtOptions>()
                .BindConfiguration("Jwt")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
