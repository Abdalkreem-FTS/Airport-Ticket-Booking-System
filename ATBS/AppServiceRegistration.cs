using ATBS.Abstractions;
using ATBS.ConsoleUI;
using ATBS.ConsoleUI.Screens;
using ATBS.Models;
using ATBS.Services;
using ATBS.Storage;
using ATBS.Storage.Repositories;
using ATBS.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace ATBS;

public static class AppServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var dataDirectory = Path.Combine(ProjectPathResolver.GetProjectDirectory(), "Data");
            return new FilePaths(dataDirectory);
        });

        services.AddSingleton<IFileStorage, JsonFileStorage>();
        services.AddSingleton<IFlightRepository, FlightRepository>();
        services.AddSingleton<IBookingRepository, BookingRepository>();
        services.AddSingleton<IPassengerRepository, PassengerRepository>();
        services.AddSingleton<IValidator<Flight>, FlightValidator>();
        services.AddSingleton<IFlightService, FlightService>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<IManagerBookingService, ManagerBookingService>();
        services.AddSingleton<IFlightImportService, FlightImportService>();
        services.AddSingleton<IValidationMetadataService, ValidationMetadataService>();

        services.AddSingleton<ConsoleApp>();
        services.AddSingleton<PassengerMenuScreen>();
        services.AddSingleton<ManagerMenuScreen>();
        services.AddSingleton<FlightSearchScreen>();
        services.AddSingleton<BookFlightScreen>();
        services.AddSingleton<PassengerBookingsScreen>();
        services.AddSingleton<ModifyBookingScreen>();
        services.AddSingleton<CancelBookingScreen>();
        services.AddSingleton<ManagerBookingFilterScreen>();
        services.AddSingleton<FlightImportScreen>();
        services.AddSingleton<ValidationRulesScreen>();

        return services;
    }

    public static async Task<IServiceProvider> SeedDataAsync(this IServiceProvider serviceProvider)
    {
        var fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        var filePaths = serviceProvider.GetRequiredService<FilePaths>();
        var flightRepository = serviceProvider.GetRequiredService<IFlightRepository>();
        var passengerRepository = serviceProvider.GetRequiredService<IPassengerRepository>();

        if (!(await passengerRepository.GetAllAsync()).Any())
        {
            foreach (var passenger in await fileStorage.LoadAsync<Passenger>(Path.Combine(filePaths.SeedDirectory, "passengers.json")))
            {
                await passengerRepository.AddAsync(passenger);
            }
        }

        if ((await flightRepository.GetAllAsync()).Any())
        {
            return serviceProvider;
        }

        await flightRepository.AddRangeAsync(await fileStorage.LoadAsync<Flight>(Path.Combine(filePaths.SeedDirectory, "flights.json")));

        return serviceProvider;
    }
}
