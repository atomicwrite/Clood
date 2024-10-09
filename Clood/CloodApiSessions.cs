using System.Collections.Concurrent;

namespace Clood;

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
}