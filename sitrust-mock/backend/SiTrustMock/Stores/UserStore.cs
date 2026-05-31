namespace SiTrustMock;

public class UserStore
{
    private static readonly Dictionary<string, UserData> _users = new()
    {
        ["1111111111111"] = new UserData("Adnan", "Bratanović", "1111111111111", "+38641000001",
            "adnan.bratanovic@example.com", "1990-03-15", "Slovenska 1", "1000", "Ljubljana"),
        ["2222222222222"] = new UserData("Edvin", "Bečič", "2222222222222", "+38641000002",
            "edvin.becic@semantika.si", "1995-01-01", "Taborska 8", "2000", "Maribor"),
        ["3333333333333"] = new UserData("Emir", "Ribič", "3333333333333", "+38641000003",
            "emir.ribic@example.com", "1992-07-22", "Maistrova 5", "2000", "Maribor"),
    };

    public UserData? FindByEmso(string emso) =>
        _users.TryGetValue(emso, out var user) ? user : null;
}
