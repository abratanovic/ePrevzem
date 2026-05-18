using ePrevzem.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace ePrevzem.Infrastructure.Identity;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly object Dummy = new();
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string plaintext) => _inner.HashPassword(Dummy, plaintext);

    public PasswordVerification Verify(string hash, string plaintext) =>
        _inner.VerifyHashedPassword(Dummy, hash, plaintext) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.NeedsRehash,
            _ => PasswordVerification.Failed
        };
}
