using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Auth;
using EventHub.Persistence.Data.Contexts;
using EventHub.WebAPI.Presentation.Mapping;
using EventHub.WebAPI.Presentation.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EventHub.WebAPI.Presentation.Extensions
{
    public static class PresentationContainer
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddHttpContextAccessor();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Register Middlewares
            services.AddTransient<GlobalErrorHandlerMiddleware>();
            services.AddTransient<TransactionMiddleware>();

            // AutoMapper
            services.AddAutoMapper(m => m.AddProfile(new AuthViewModelProfile()));
            services.AddAutoMapper(m => m.AddProfile(new AccountViewModelProfile()));
            services.AddAutoMapper(m => m.AddProfile(new EventViewModelProfile()));
            services.AddAutoMapper(m => m.AddProfile(new CategoryViewModelProfile()));

            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<EventDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            });

            // JWT Authentication
            var settings = configuration.GetSection("Jwt").Get<JwtOptions>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings?.Issuer,
                    ValidAudience = settings?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings?.Key!)),
                };
            });

            return services;
        }
        public static IApplicationBuilder UseCustomMiddlewares(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<GlobalErrorHandlerMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<TransactionMiddleware>();

            app.MapControllers();

            return app;
        }
        public static async Task UseDbInitializer(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.IntiliazeAsync();
        }
    }
}
