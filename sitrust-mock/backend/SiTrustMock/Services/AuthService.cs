namespace SiTrustMock;

public class AuthService(AuthAttemptStore store, UserStore users, IQrCodeGenerator qr, JwtService jwt) : IAuthService
{
    public InitiateResult Initiate(string? redirectUrl, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
            return new InitiateResult.InvalidRequest("redirectUrl is required");

        var attemptId = store.Create(redirectUrl);
        var completeUrl = $"{baseUrl}/api/auth/complete?attemptId={attemptId}";
        return new InitiateResult.Success(attemptId, qr.Generate(completeUrl));
    }

    public CompleteResult Complete(string? attemptId, string? emso)
    {
        if (string.IsNullOrWhiteSpace(attemptId))
            return new CompleteResult.InvalidRequest("attemptId is required");

        if (string.IsNullOrWhiteSpace(emso))
            return new CompleteResult.InvalidRequest("emso is required");

        var user = users.FindByEmso(emso);
        if (user is null)
            return new CompleteResult.UserNotFound();

        return store.Complete(attemptId, user)
            ? new CompleteResult.Success()
            : new CompleteResult.AttemptNotFound();
    }

    public AuthCheckResult Check(string attemptId) =>
        store.Check(attemptId) switch
        {
            CheckResult.NotFound => new AuthCheckResult.NotFound(),
            CheckResult.Pending => new AuthCheckResult.Pending(),
            CheckResult.Complete r => new AuthCheckResult.Complete($"{r.RedirectUrl}?token={jwt.Sign(r.UserData)}"),
            _ => throw new InvalidOperationException("Unexpected CheckResult type")
        };
}
