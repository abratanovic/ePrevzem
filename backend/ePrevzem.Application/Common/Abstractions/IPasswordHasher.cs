namespace ePrevzem.Application.Common.Abstractions;

public interface IPasswordHasher
{
    string Hash(string plaintext);
    PasswordVerification Verify(string hash, string plaintext);
}

public enum PasswordVerification { Failed, Success, NeedsRehash }
