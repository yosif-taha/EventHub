using EventHub.Application.Common.Behaviors;
using EventHub.Application.Common.Mapping.Category;
using EventHub.Application.Common.Mapping.Events;
using EventHub.Application.Common.Mapping.Registrations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EventHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // AutoMapper
            services.AddAutoMapper(m => m.AddProfile(new EventProfile()));
            services.AddAutoMapper(m => m.AddProfile(new CategoryProfile()));
            services.AddAutoMapper(m => m.AddProfile(new RegistrationProfile()));

            return services;
        }
    }
}
