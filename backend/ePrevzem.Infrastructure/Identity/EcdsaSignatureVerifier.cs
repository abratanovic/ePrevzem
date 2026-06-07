using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;

namespace ePrevzem.Infrastructure.Identity;

public sealed class EcdsaSignatureVerifier : ISignatureVerifier
{
    public bool Verify(string publicKeyPem, byte[] data, byte[] signatureDer)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);
            return ecdsa.VerifyData(data, signatureDer,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException) { return false; }
    }
}
