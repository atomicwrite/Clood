using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Clood;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class FromSessionAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource => BindingSource.Custom;
}