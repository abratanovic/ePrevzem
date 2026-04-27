using SiTrustMock;

namespace SiTrustMock.Tests;

internal static class TestData
{
    internal static UserData SampleUser() => new("Ana", "Novak", "1234567890123", "+38641000000",
        "ana@example.com", "1990-01-01", "Slovenska 1", "1000", "Ljubljana");
}
