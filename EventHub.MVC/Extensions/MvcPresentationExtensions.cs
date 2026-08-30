using EventHub.MVC.Options;
using EventHub.MVC.Services;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Admins;
using EventHub.MVC.Services.Auth;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Organizers;
using EventHub.MVC.Services.Registrations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace EventHub.MVC.Extensions;

public static class MvcPresentationExtensions
{
    public static IServiceCollection AddMvcPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();

        services.AddOptions<BackendApiOptions>()
            .BindConfiguration(BackendApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddTransient<BackendBearerTokenHandler>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.Cookie.Name = "EventHub.Mvc.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
            });

        services.AddAuthorization();

        services.AddHttpClient(AuthApiClient.HttpClientName, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        });

        services.AddHttpClient(BackendApiClientNames.Public, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        });

        services.AddHttpClient(BackendApiClientNames.Authenticated, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        }).AddHttpMessageHandler<BackendBearerTokenHandler>();

        services.AddScoped<IAuthApiClient, AuthApiClient>();
        services.AddScoped<IBackendApiClient, BackendApiClient>();
        services.AddScoped<IMvcAuthenticationService, MvcAuthenticationService>();
        services.AddScoped<IEventApiClient, EventApiClient>();
        services.AddScoped<IRegistrationApiClient, RegistrationApiClient>();
        services.AddScoped<IOrganizerApiClient, OrganizerApiClient>();
        services.AddScoped<IAdminApiClient, AdminApiClient>();

        return services;
    }

    public static WebApplication UseMvcPresentation(this WebApplication app)
    {
        app.UseExceptionHandler("/Home/Error");

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?statusCode={0}");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
