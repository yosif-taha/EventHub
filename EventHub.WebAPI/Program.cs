using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Account;
using EventHub.Infrastructure.Auth;
using EventHub.Infrastructure.Email;
using EventHub.Persistence.Data.Contexts;
using EventHub.Persistence.DataSeeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<EventDbContext>( options => 
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                );

            //***********
            // Register Any services
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<IAuthService,AuthService>();
            builder.Services.AddSingleton<IJwtProvider,JwtProvider>();
            builder.Services.AddScoped<IEmailService,EmailService>();
            builder.Services.AddScoped<IAccountService,AccountService>();
            builder.Services.AddScoped<IDbInitializer,DbInitializer>();
            //************** end

            //****************
            // Different Configurations
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(nameof(EmailSettings)));
            builder.Services.AddHttpContextAccessor();
            //**************** end

            //************** 
            // Configure JwtOptions with validation
            builder.Services.AddOptions<JwtOptions>()
                .BindConfiguration("Jwt")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Retrieve JwtOptions for use in JWT authentication configuration  
            var settings = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
            //************** end

            //**************
            // Configure Identity with Entity Framework stores
            builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<EventDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity options (e.g., password requirements, user settings)
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            });

            // Configure JWT authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(O =>
            {
                O.SaveToken = true;
                O.TokenValidationParameters = new TokenValidationParameters
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
            //************** end

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Cofiguration of Data Sedding
            var scope = app.Services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.IntiliazeAsync();
            app.Run();
        }
    }
}
