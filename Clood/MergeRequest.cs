using Microsoft.Extensions.DependencyInjection;

namespace Clood;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Serilog;
public class MergeRequest
{
    public string Id { get; set; }
    
}

public class DiscardRequest
{
    public string Id { get; set; }
    
}
public interface IHasIdString
{
    string Id { get; }
}

public interface IHasSuccess
{
    bool Success { get; set; }
    string? ErrorMessage { get; set; }
}
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class FromSessionAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource => BindingSource.Custom;
}

public class SessionBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(CloodSession))
        {
            return new SessionBinder();
        }

        return null;
    }
}
public static class SessionBindingExtensions
{
    public static void AddSessionBinding(this IServiceCollection services)
    {
        services.AddSingleton<IModelBinderProvider, SessionBinderProvider>();
    }
}


public class SessionBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var model = bindingContext.Model as IHasIdString;
        var id = model?.Id ?? bindingContext.ValueProvider.GetValue("Id").FirstValue;

        if (string.IsNullOrEmpty(id))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        if (!CloodApiSessions.TryRemove(id, out var session) || session == null)
        {
            Log.Warning("Session not found: {SessionId}", id);
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        bindingContext.Result = ModelBindingResult.Success(session);
    }
}
