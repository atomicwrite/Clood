using Clood.Endpoints.DTO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;

namespace Clood.Session;

public class SessionBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var model = bindingContext.Model as IHasIdString;
        var id = model?.Id ?? bindingContext.ValueProvider.GetValue("Id").FirstValue;

        if (string.IsNullOrEmpty(id))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        if (!CloodApiSessions.TryRemove(id, out var session) || session == null)
        {
            Log.Warning("Session not found: {SessionId}", id);
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(session);
        return Task.CompletedTask;
    }
}