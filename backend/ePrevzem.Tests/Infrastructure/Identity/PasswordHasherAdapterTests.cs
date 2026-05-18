using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Infrastructure.Identity;
using FluentAssertions;

namespace ePrevzem.Tests.Infrastructure.Identity;

public class PasswordHasherAdapterTests
{
    [Fact]
    public void Hash_returns_non_empty_hash_that_differs_from_plaintext()
    {
        var adapter = new PasswordHasherAdapter();

        var hash = adapter.Hash("ChangeMe!1");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("ChangeMe!1");
    }

    [Fact]
    public void Verify_returns_success_for_matching_plaintext()
    {
        var adapter = new PasswordHasherAdapter();
        var hash = adapter.Hash("ChangeMe!1");

        var result = adapter.Verify(hash, "ChangeMe!1");

        result.Should().BeOneOf(PasswordVerification.Success, PasswordVerification.NeedsRehash);
    }

    [Fact]
    public void Verify_returns_failed_for_non_matching_plaintext()
    {
        var adapter = new PasswordHasherAdapter();
        var hash = adapter.Hash("ChangeMe!1");

        var result = adapter.Verify(hash, "wrong-password");

        result.Should().Be(PasswordVerification.Failed);
    }
}
