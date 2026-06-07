using System.Security.Cryptography;
using System.Text;
using ePrevzem.Infrastructure.Identity;
using Xunit;

namespace ePrevzem.Tests.Infrastructure.Identity;

public class EcdsaSignatureVerifierTests
{
    [Fact]
    public void Verifies_valid_p256_der_signature_over_challenge()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var pem = new string(PemEncoding.Write("PUBLIC KEY", spki));
        var challenge = Encoding.UTF8.GetBytes("hello-challenge");
        var sig = ecdsa.SignData(challenge, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        var verifier = new EcdsaSignatureVerifier();
        Assert.True(verifier.Verify(pem, challenge, sig));
        challenge[0] ^= 0xFF;
        Assert.False(verifier.Verify(pem, challenge, sig));
    }

    [Fact]
    public void Returns_false_for_garbage_signature()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = new string(PemEncoding.Write("PUBLIC KEY", ecdsa.ExportSubjectPublicKeyInfo()));
        var verifier = new EcdsaSignatureVerifier();
        Assert.False(verifier.Verify(pem, Encoding.UTF8.GetBytes("x"), new byte[] { 1, 2, 3 }));
    }
}
