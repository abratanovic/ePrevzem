namespace SiTrustMock;

public interface IAuthService
{
    InitiateResult Initiate(string? redirectUrl, string baseUrl);
    CompleteResult Complete(string? attemptId, string? emso);
    AuthCheckResult Check(string attemptId);
}
