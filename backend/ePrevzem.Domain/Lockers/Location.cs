namespace ePrevzem.Domain.Lockers;

public sealed record Location
{
    public decimal Latitude { get; }
    public decimal Longitude { get; }
    public string Address { get; }
    public string HouseNumber { get; }
    public string ZipCode { get; }
    public string City { get; }

    private Location(
        decimal latitude,
        decimal longitude,
        string address,
        string houseNumber,
        string zipCode,
        string city)
    {
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        HouseNumber = houseNumber;
        ZipCode = zipCode;
        City = city;
    }

    public static Location Create(
        decimal latitude,
        decimal longitude,
        string address,
        string houseNumber,
        string zipCode,
        string city)
    {
        if (latitude < -90m || latitude > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be in [-90, 90].");
        if (longitude < -180m || longitude > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be in [-180, 180].");
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
        if (string.IsNullOrWhiteSpace(houseNumber))
            throw new ArgumentException("House number is required.", nameof(houseNumber));
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code is required.", nameof(zipCode));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        return new Location(latitude, longitude, address, houseNumber, zipCode, city);
    }
}
