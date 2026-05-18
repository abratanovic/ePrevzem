namespace ePrevzem.Domain.Identity;

public sealed record PersonalInfo
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Email { get; private set; }

    private PersonalInfo() { }

    public static PersonalInfo Create(string firstName, string lastName, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        return new PersonalInfo { FirstName = firstName, LastName = lastName, Email = email };
    }
}
