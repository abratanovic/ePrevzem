using System.Collections.Concurrent;

namespace SiTrustMock;

public class AuthAttemptStore
{
    private readonly ConcurrentDictionary<string, AuthAttempt> _attempts = new();

    public string Create(string redirectUrl)
    {
        var id = Guid.NewGuid().ToString();
        _attempts[id] = new AuthAttempt(redirectUrl);
        return id;
    }

    public AuthAttempt? Get(string attemptId) =>
        _attempts.TryGetValue(attemptId, out var attempt) ? attempt : null;

    public bool Complete(string attemptId, UserData userData)
    {
        if (!_attempts.TryGetValue(attemptId, out var attempt)) return false;
        _attempts[attemptId] = attempt with { State = AuthAttemptState.Complete, UserData = userData };
        return true;
    }
}
