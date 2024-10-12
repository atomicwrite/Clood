using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Clood.Session;

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