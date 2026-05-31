namespace SiTrustMock;

public class UserStore
{
    private static readonly Dictionary<string, UserData> _users = new()
    {
        ["1234567890123"] = new UserData("Ana", "Novak", "1234567890123", "+38641000001",
            "ana.novak@example.com", "1990-01-15", "Slovenska 1", "1000", "Ljubljana"),
        ["9876543210987"] = new UserData("Janez", "Kranjski", "9876543210987", "+38641000002",
            "janez.kranjski@example.com", "1985-06-20", "Maistrova 5", "2000", "Maribor"),
        ["1111111111111"] = new UserData("Maja", "Horvat", "1111111111111", "+38641000003",
            "maja.horvat@example.com", "1995-11-03", "Prešernova 10", "6000", "Koper"),
        ["2222222222222"] = new UserData("Edvin", "Bečič", "2222222222222", "+38641000004",
            "edvin.becic@semantika.si", "1995-01-01", "Taborska 8", "2000", "Maribor"),
    };

    public UserData? FindByEmso(string emso) =>
        _users.TryGetValue(emso, out var user) ? user : null;
}
