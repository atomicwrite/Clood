using System.Collections.Concurrent;
using Clood.Endpoints.API;
using Serilog;

namespace Clood.Session;

public static class CloodApiSessions
{
    private static readonly ConcurrentDictionary<string, CloodSession> Sessions = new();

    public static bool TryAddSession(string sessionId, CloodSession session)
    {
        return Sessions.TryAdd(sessionId, session);
    }

    public static bool TryRemove(string id, out CloodSession? session)
    {
        return Sessions.TryRemove(id, out session);
    }

    public static CloodSession CreateSession(  bool useGit,   List<string> filesList)
    {
        var sessionId = Guid.NewGuid().ToString();
        Log.Debug("Generated new session ID: {SessionId}", sessionId);
        return new CloodSession
        {
            Id = sessionId,
            UseGit = useGit,
            GitRoot =  CloodApi.GitRoot,
            Files = filesList
        };
    }
}