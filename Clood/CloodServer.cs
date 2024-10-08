using Microsoft.AspNetCore.Builder;

namespace Clood;

public class CloodServer
{
    public static void Start(string? url)
    {
        var builder = WebApplication.CreateBuilder();

        var app = builder.Build();

        CloodApi.ConfigureApi(app);
        if (!string.IsNullOrWhiteSpace(url))
            app.Run(url);
        else
            app.Run();
    }
}