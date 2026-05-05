namespace SiTrustMock;

public abstract record InitiateResult
{
    public sealed record Success(string AttemptId, string QrCodeImage) : InitiateResult;
    public sealed record InvalidRequest(string Error) : InitiateResult;
}

public abstract record CompleteResult
{
    public sealed record Success : CompleteResult;
    public sealed record AttemptNotFound : CompleteResult;
    public sealed record UserNotFound : CompleteResult;
    public sealed record InvalidRequest(string Error) : CompleteResult;
}

public abstract record AuthCheckResult
{
    public sealed record NotFound : AuthCheckResult;
    public sealed record Pending : AuthCheckResult;
    public sealed record Complete(string RedirectUrl) : AuthCheckResult;
}
