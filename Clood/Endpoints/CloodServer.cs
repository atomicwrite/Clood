using Clood.Endpoints.API;
using Clood.Session;
using Microsoft.AspNetCore.Builder;

namespace Clood.Endpoints;

public class CloodServer
{
    public static void Start(string? url,string gitRoot)
    {
        var builder = WebApplication.CreateBuilder();
       
        var app = builder.Build();

        CloodApi.ConfigureApi(app,gitRoot);
        if (!string.IsNullOrWhiteSpace(url))
            app.Run(url);
        else
            app.Run();
    }
}