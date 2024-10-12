using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;

namespace Clood;

public class SessionBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

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