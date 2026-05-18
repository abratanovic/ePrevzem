using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class LocationTests
{
    [Fact]
    public void Create_with_valid_fields_constructs_value_object()
    {
        var location = Location.Create(
            latitude: 46.0569m,
            longitude: 14.5058m,
            address: "Slovenska cesta",
            houseNumber: "11",
            zipCode: "1000",
            city: "Ljubljana");

        location.Latitude.Should().Be(46.0569m);
        location.Longitude.Should().Be(14.5058m);
        location.Address.Should().Be("Slovenska cesta");
        location.HouseNumber.Should().Be("11");
        location.ZipCode.Should().Be("1000");
        location.City.Should().Be("Ljubljana");
    }

    [Theory]
    [InlineData(-90.001)]
    [InlineData(90.001)]
    public void Create_with_out_of_range_latitude_throws(decimal latitude)
    {
        var act = () => Location.Create(latitude, 0m, "a", "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("latitude");
    }

    [Theory]
    [InlineData(-180.001)]
    [InlineData(180.001)]
    public void Create_with_out_of_range_longitude_throws(decimal longitude)
    {
        var act = () => Location.Create(0m, longitude, "a", "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("longitude");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_address_throws(string address)
    {
        var act = () => Location.Create(0m, 0m, address, "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentException>().WithParameterName("address");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_city_throws(string city)
    {
        var act = () => Location.Create(0m, 0m, "a", "1", "1000", city);
        act.Should().Throw<ArgumentException>().WithParameterName("city");
    }

    [Fact]
    public void Two_locations_with_same_values_are_equal()
    {
        var a = Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");
        var b = Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");
        a.Should().Be(b);
    }
}
