using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Clood;

public static class SessionBindingExtensions
{
    public static void AddSessionBinding(this IServiceCollection services)
    {
        services.AddSingleton<IModelBinderProvider, SessionBinderProvider>();
    }
}