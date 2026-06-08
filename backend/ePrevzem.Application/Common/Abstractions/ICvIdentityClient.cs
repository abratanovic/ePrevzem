namespace ePrevzem.Application.Common.Abstractions;

public sealed record SelfieFrame(byte[] Data, string FileName);

public sealed record CvIdentityVerifyResult(
    bool Verified,
    string? FirstName,
    string? LastName,
    string? Emso,
    IReadOnlyList<string> Reasons);

public interface ICvIdentityClient
{
    Task<CvIdentityVerifyResult> VerifyAsync(
        byte[] idFront,
        string idFrontFileName,
        IReadOnlyList<SelfieFrame> selfieFrames,
        string variant,
        CancellationToken cancellationToken = default);
}

public sealed class CvIdentityUnavailableException : Exception
{
    public CvIdentityUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public sealed class DocumentVerificationFailedException : Exception
{
    public IReadOnlyList<string> Reasons { get; }

    public DocumentVerificationFailedException(IReadOnlyList<string> reasons)
        : base("Document verification failed.")
        => Reasons = reasons;
}
