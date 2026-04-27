using SiTrustMock;

namespace SiTrustMock.Tests;

public class AuthAttemptStoreTests
{
    private static AuthAttemptStore MakeStore() => new();

    private static UserData SampleUser() => new("Ana", "Novak", "1234567890123", "+38641000000",
        "ana@example.com", "1990-01-01", "Slovenska 1", "1000", "Ljubljana");

    [Fact]
    public void Create_ReturnsNonEmptyUuid()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public void Get_NewAttempt_ReturnsPendingState()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        var attempt = store.Get(id);
        Assert.NotNull(attempt);
        Assert.Equal(AuthAttemptState.Pending, attempt.State);
    }

    [Fact]
    public void Complete_TransitionsToCompleteAndStoresUserData()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        var user = SampleUser();
        var result = store.Complete(id, user);
        Assert.True(result);
        var attempt = store.Get(id);
        Assert.Equal(AuthAttemptState.Complete, attempt!.State);
        Assert.Equal(user, attempt.UserData);
    }

    [Fact]
    public void Get_AfterComplete_ReturnsUserData()
    {
        var store = MakeStore();
        var id = store.Create("https://example.com/callback");
        var user = SampleUser();
        store.Complete(id, user);
        var attempt = store.Get(id);
        Assert.NotNull(attempt!.UserData);
        Assert.Equal("Ana", attempt.UserData!.FirstName);
    }

    [Fact]
    public void Complete_UnknownAttemptId_ReturnsFalse()
    {
        var store = MakeStore();
        var result = store.Complete(Guid.NewGuid().ToString(), SampleUser());
        Assert.False(result);
    }

    [Fact]
    public void MultipleAttempts_AreIndependent()
    {
        var store = MakeStore();
        var id1 = store.Create("https://a.com");
        var id2 = store.Create("https://b.com");
        store.Complete(id1, SampleUser());
        Assert.Equal(AuthAttemptState.Complete, store.Get(id1)!.State);
        Assert.Equal(AuthAttemptState.Pending, store.Get(id2)!.State);
    }
}
