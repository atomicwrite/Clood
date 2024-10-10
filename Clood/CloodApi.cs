using System.Collections.Concurrent;
using CliWrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clood;

public static class CloodApi
{
    public static string GitRoot { get; set; } = null!;
    public static void ConfigureApi(WebApplication app, string gitRoot)
    {
        app.UseExceptionHandler()(ap)
        app.MapPost("/api/clood/start", CreateCloodApi.CreateCloodChanges);
        app.MapPost("/api/clood/merge", MergeCloodApi.MergeCloodChanges);
        app.MapPost("/api/clood/revert", RevertCloodApi.RevertCloodChanges);
        GitRoot = gitRoot; 
    }
}