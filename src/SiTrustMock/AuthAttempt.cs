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
);

public record AuthAttempt(
    string RedirectUrl,
    AuthAttemptState State = AuthAttemptState.Pending,
    UserData? UserData = null
);
