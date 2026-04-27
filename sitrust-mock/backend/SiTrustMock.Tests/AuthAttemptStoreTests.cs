using SiTrustMock;

namespace SiTrustMock.Tests;

public class AuthAttemptStoreTests
{
    private static AuthAttemptStore MakeStore() => new();

    [Fact]
    public void Create_ReturnsNonEmptyUuid()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public void Check_NewAttempt_ReturnsPending()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        Assert.IsType<CheckResult.Pending>(store.Check(id));
    }

    [Fact]
    public void Check_AfterComplete_ReturnsCompleteWithUserDataAndRedirectUrl()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        var user = TestData.SampleUser();
        store.Complete(id, user);
        var result = Assert.IsType<CheckResult.Complete>(store.Check(id));
        Assert.Equal(user, result.UserData);
        Assert.Equal("https://example.com/callback", result.RedirectUrl);
    }

    [Fact]
    public void Check_UnknownAttemptId_ReturnsNotFound()
    {
        var store = MakeStore();
        Assert.IsType<CheckResult.NotFound>(store.Check(Guid.NewGuid().ToString()));
    }

    [Fact]
    public void Complete_UnknownAttemptId_ReturnsFalse()
    {
        var store = MakeStore();
        Assert.False(store.Complete(Guid.NewGuid().ToString(), TestData.SampleUser()));
    }

    [Fact]
    public void MultipleAttempts_AreIndependent()
    {
        var store = MakeStore();
        var id1 = store.Create("https://a.com");
        var id2 = store.Create("https://b.com");
        store.Complete(id1, TestData.SampleUser());
        Assert.IsType<CheckResult.Complete>(store.Check(id1));
        Assert.IsType<CheckResult.Pending>(store.Check(id2));
    }
}
