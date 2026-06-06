using System.Reflection;
using FluentValidation;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Common.Behaviors;
using ePrevzem.Application.Common.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ePrevzem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
