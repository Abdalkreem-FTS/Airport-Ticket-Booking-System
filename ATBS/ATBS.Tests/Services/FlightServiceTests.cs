using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Services;
using ATBS.Tests.TestSupport;
using Moq;

namespace ATBS.Tests.Services;

public sealed class FlightServiceTests
{
    private readonly Mock<IFlightRepository> _flights = new();

    private FlightService CreateService() => new(_flights.Object);

    private void GivenFlights(params Flight[] flights) =>
        _flights.Setup(f => f.GetAllAsync()).ReturnsAsync(Builders.Ok<IReadOnlyList<Flight>>(flights.ToList()));

    [Fact]
    public async Task SearchAvailableFlightsAsync_ExcludesPastFlights_AndOrdersByDepartureAscending()
    {
        var past = Builders.NewFlight(departureDate: DateTimeOffset.UtcNow.AddDays(-1));
        var later = Builders.NewFlight(departureDate: DateTimeOffset.UtcNow.AddDays(10));
        var soon = Builders.NewFlight(departureDate: DateTimeOffset.UtcNow.AddDays(2));
        GivenFlights(later, soon, past);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria());

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(flight => flight.Id).Should().Equal(soon.Id, later.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_MatchesDepartureCountry_CaseInsensitivelyAndTrimmed()
    {
        var jordan = Builders.NewFlight(departureCountry: "Jordan");
        var egypt = Builders.NewFlight(departureCountry: "Egypt");
        GivenFlights(jordan, egypt);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            DepartureCountry = "  jORdan  "
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(jordan.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_FiltersByDestinationCountry()
    {
        var toFrance = Builders.NewFlight(destinationCountry: "France");
        var toSpain = Builders.NewFlight(destinationCountry: "Spain");
        GivenFlights(toFrance, toSpain);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            DestinationCountry = "France"
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(toFrance.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_FiltersByDepartureDate()
    {
        var onDate = Builders.NewFlight(departureDate: new DateTimeOffset(2030, 6, 15, 9, 0, 0, TimeSpan.Zero));
        var otherDate = Builders.NewFlight(departureDate: new DateTimeOffset(2030, 6, 16, 9, 0, 0, TimeSpan.Zero));
        GivenFlights(onDate, otherDate);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            DepartureDate = new DateOnly(2030, 6, 15)
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(onDate.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_FiltersByAirports()
    {
        var match = Builders.NewFlight(departureAirport: "AMM", arrivalAirport: "CDG");
        var wrongDeparture = Builders.NewFlight(departureAirport: "DXB", arrivalAirport: "CDG");
        var wrongArrival = Builders.NewFlight(departureAirport: "AMM", arrivalAirport: "JFK");
        GivenFlights(match, wrongDeparture, wrongArrival);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            DepartureAirport = "AMM",
            ArrivalAirport = "CDG"
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_FiltersByClass_RequiringAvailableSeats()
    {
        var businessAvailable = Builders.NewFlight(classPrices: [Builders.NewClassPrice(FlightClass.Business, availableSeats: 2)]);
        var businessSoldOut = Builders.NewFlight(classPrices: [Builders.NewClassPrice(FlightClass.Business, availableSeats: 0)]);
        var economyOnly = Builders.NewFlight(classPrices: [Builders.NewClassPrice(availableSeats: 5)]);
        GivenFlights(businessAvailable, businessSoldOut, economyOnly);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            Class = FlightClass.Business
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(businessAvailable.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_FiltersByMaxPrice_AcrossAvailableClasses()
    {
        var affordable = Builders.NewFlight(classPrices: [Builders.NewClassPrice(price: 150m, availableSeats: 4)]);
        var tooExpensive = Builders.NewFlight(classPrices: [Builders.NewClassPrice(price: 900m, availableSeats: 4)]);
        var cheapButSoldOut = Builders.NewFlight(classPrices: [Builders.NewClassPrice(price: 50m, availableSeats: 0)]);
        GivenFlights(affordable, tooExpensive, cheapButSoldOut);

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria
        {
            MaxPrice = 200m
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(affordable.Id);
    }

    [Fact]
    public async Task SearchAvailableFlightsAsync_PropagatesRepositoryError()
    {
        _flights.Setup(f => f.GetAllAsync()).ReturnsAsync(Builders.Fail<IReadOnlyList<Flight>>(Error.Failure("Flights.LoadFailed", "io")));

        var result = await CreateService().SearchAvailableFlightsAsync(new FlightSearchCriteria());

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Flights.LoadFailed");
    }

    [Fact]
    public async Task GetFlightByIdAsync_DelegatesToRepository()
    {
        var flightId = Guid.NewGuid();
        var flight = Builders.NewFlight(id: flightId);
        _flights.Setup(f => f.GetByIdAsync(flightId)).ReturnsAsync(Builders.Ok(flight));

        var result = await CreateService().GetFlightByIdAsync(flightId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(flightId);
        _flights.Verify(f => f.GetByIdAsync(flightId), Times.Once);
    }
}
