namespace SiTrustMock;

public enum AuthAttemptState { Pending, Complete }

public record UserData(
    string FirstName,
    string LastName,
    string Emso,
    string Phone,
    string Email,
    string DateOfBirth,
    string Address,
    string Zip,
    string City
)
{
    public static class Claims
    {
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
        public const string Emso = "emso";
        public const string Phone = "phone";
        public const string Email = "email";
        public const string DateOfBirth = "dateOfBirth";
        public const string Address = "address";
        public const string Zip = "zip";
        public const string City = "city";
    }
}

public record AuthAttempt(
    string RedirectUrl,
    AuthAttemptState State = AuthAttemptState.Pending,
    UserData? UserData = null
);

public abstract record CheckResult
{
    public sealed record NotFound : CheckResult;
    public sealed record Pending : CheckResult;
    public sealed record Complete(UserData UserData, string RedirectUrl) : CheckResult;
}

