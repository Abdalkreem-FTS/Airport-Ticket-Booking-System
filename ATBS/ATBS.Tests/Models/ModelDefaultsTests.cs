using ATBS.Console.Models;
using ATBS.Console.Models.Enums;

namespace ATBS.Tests.Models;

public sealed class ModelDefaultsTests
{
    [Fact]
    public void Booking_Defaults_ToConfirmed_WithGeneratedId()
    {
        var booking = new Booking();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    [Fact]
    public void Flight_Defaults_ToEmptyClassPrices_WithGeneratedId()
    {
        var flight = new Flight();

        Assert.NotEqual(Guid.Empty, flight.Id);
        Assert.Empty(flight.ClassPrices);
    }

    [Fact]
    public void Passenger_And_Manager_ShareUserIdentity()
    {
        Assert.IsType<User>(new Passenger(), exactMatch: false);
        Assert.IsType<User>(new Manager(), exactMatch: false);
    }
}
