using ATBS.Console.Models;
using ATBS.Console.Models.Enums;

namespace ATBS.Tests.Models;

public sealed class ModelDefaultsTests
{
    [Fact]
    public void Booking_Defaults_ToConfirmed_WithGeneratedId()
    {
        var booking = new Booking();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Flight_Defaults_ToEmptyClassPrices_WithGeneratedId()
    {
        var flight = new Flight();

        flight.Id.Should().NotBe(Guid.Empty);
        flight.ClassPrices.Should().BeEmpty();
    }

    [Fact]
    public void Passenger_And_Manager_ShareUserIdentity()
    {
        new Passenger().Should().BeAssignableTo<User>();
        new Manager().Should().BeAssignableTo<User>();
    }
}
