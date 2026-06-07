namespace ePrevzem.Application.Common.Abstractions;

public interface ISignatureVerifier
{
    bool Verify(string publicKeyPem, byte[] data, byte[] signatureDer);
}
