using ATBS.Abstractions;
using ATBS.Models;
using ATBS.Models.Enums;
using ATBS.Services;
using ATBS.Storage;
using ATBS.Storage.Repositories;
using ATBS.Validation;

namespace ATBS.Composition;

public sealed class AppServices
{
    public IFlightRepository FlightRepository { get; }
    public IBookingRepository BookingRepository { get; }
    public IPassengerRepository PassengerRepository { get; }
    public IFlightService FlightService { get; }
    public IBookingService BookingService { get; }
    public IManagerBookingService ManagerBookingService { get; }
    public IFlightImportService FlightImportService { get; }
    public IValidationMetadataService ValidationMetadataService { get; }

    private AppServices(
        IFlightRepository flightRepository,
        IBookingRepository bookingRepository,
        IPassengerRepository passengerRepository,
        IFlightService flightService,
        IBookingService bookingService,
        IManagerBookingService managerBookingService,
        IFlightImportService flightImportService,
        IValidationMetadataService validationMetadataService)
    {
        FlightRepository = flightRepository;
        BookingRepository = bookingRepository;
        PassengerRepository = passengerRepository;
        FlightService = flightService;
        BookingService = bookingService;
        ManagerBookingService = managerBookingService;
        FlightImportService = flightImportService;
        ValidationMetadataService = validationMetadataService;
    }

    public static AppServices Create()
    {
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        var filePaths = new FilePaths(dataDirectory);
        IFileStorage fileStorage = new JsonFileStorage();

        IFlightRepository flightRepository = new FlightRepository(fileStorage, filePaths);
        IBookingRepository bookingRepository = new BookingRepository(fileStorage, filePaths);
        IPassengerRepository passengerRepository = new PassengerRepository(fileStorage, filePaths);
        IValidator<Flight> flightValidator = new FlightValidator();

        SeedData(flightRepository, passengerRepository);

        IFlightService flightService = new FlightService(flightRepository);
        IBookingService bookingService = new BookingService(bookingRepository, flightRepository, passengerRepository);
        IManagerBookingService managerBookingService = new ManagerBookingService(bookingRepository, flightRepository);
        IFlightImportService flightImportService = new FlightImportService(flightRepository, flightValidator);
        IValidationMetadataService validationMetadataService = new ValidationMetadataService();

        return new AppServices(
            flightRepository,
            bookingRepository,
            passengerRepository,
            flightService,
            bookingService,
            managerBookingService,
            flightImportService,
            validationMetadataService);
    }

    private static void SeedData(IFlightRepository flightRepository, IPassengerRepository passengerRepository)
    {
        if (passengerRepository.GetAll().Count == 0)
        {
            passengerRepository.Add(new Passenger
            {
                FirstName = "Abdalkreem",
                LastName = "Bzoor",
                Email = "abdalkreem.bzoor@example.com",
                Country = "Palestine",
                DateOfBirth = new DateTime(2003, 7, 24)
            });
        }

        if (flightRepository.GetAll().Count > 0)
        {
            return;
        }

        flightRepository.Add(new Flight
        {
            FlightNumber = "ATBS-100",
            DepartureCountry = "Palestine",
            DestinationCountry = "Saudi Arabia",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(7),
            DepartureAirport = "PNA",
            ArrivalAirport = "SANA",
            Capacity = 160,
            ClassPrices =
            [
                new FlightClassPrice
                {
                    Class = FlightClass.Economy,
                    Price = 250,
                    AvailableSeats = 120
                },
                new FlightClassPrice
                {
                    Class = FlightClass.Business,
                    Price = 650,
                    AvailableSeats = 30
                },
                new FlightClassPrice
                {
                    Class = FlightClass.FirstClass,
                    Price = 1200,
                    AvailableSeats = 10
                }
            ]
        });
    }
}
