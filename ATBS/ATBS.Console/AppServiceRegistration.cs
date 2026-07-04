using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI;
using ATBS.Console.ConsoleUI.Screens;
using ATBS.Console.Models;
using ATBS.Console.Services;
using ATBS.Console.Storage;
using ATBS.Console.Storage.Repositories;
using ATBS.Console.Transactions;
using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.ConcurrencyControl;
using ATBS.Console.Transactions.Management;
using ATBS.Console.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ATBS.Console;

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

        services.AddSingleton<TransactionFileCatalog>();
        services.AddSingleton<TransactionLogDirectory>();
        services.AddSingleton<FileTransactionContext>();
        services.AddSingleton<ConcurrencyControlStrategyFactory>();
        services.AddSingleton<ILockManager>(_ => new LockManager());
        services.AddSingleton<IVersionStore, VersionStore>();
        services.AddSingleton<IStagedStore, StagedStore>();
        services.AddSingleton<IFileTransactionFactory, FileTransactionFactory>();
        services.AddSingleton<ITransactionalFileStorage, TransactionalJsonFileStorage>();

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

        var passengersResult = await passengerRepository.GetAllAsync();
        if (passengersResult.IsError)
        {
            RenderStartupErrors(passengersResult.Errors);
            return serviceProvider;
        }

        if (!passengersResult.Value.Any())
        {
            var seedPassengersResult = await fileStorage.LoadAsync<Passenger>(Path.Combine(filePaths.SeedDirectory, "passengers.json"));
            if (seedPassengersResult.IsError)
            {
                RenderStartupErrors(seedPassengersResult.Errors);
                return serviceProvider;
            }

            foreach (var passenger in seedPassengersResult.Value)
            {
                var addPassengerResult = await passengerRepository.AddAsync(passenger);
                if (!addPassengerResult.IsError)
                {
                    continue;
                }
                
                RenderStartupErrors(addPassengerResult.Errors);
                
                return serviceProvider;
            }
        }

        var flightsResult = await flightRepository.GetAllAsync();
        if (flightsResult.IsError)
        {
            RenderStartupErrors(flightsResult.Errors);
            return serviceProvider;
        }

        if (flightsResult.Value.Any())
        {
            return serviceProvider;
        }

        var seedFlightsResult = await fileStorage.LoadAsync<Flight>(Path.Combine(filePaths.SeedDirectory, "flights.json"));
        if (seedFlightsResult.IsError)
        {
            RenderStartupErrors(seedFlightsResult.Errors);
            return serviceProvider;
        }

        var addFlightsResult = await flightRepository.AddRangeAsync(seedFlightsResult.Value);
        if (addFlightsResult.IsError)
        {
            RenderStartupErrors(addFlightsResult.Errors);
        }

        return serviceProvider;
    }

    private static void RenderStartupErrors(IReadOnlyList<Results.Error> errors)
    {
        foreach (var error in errors)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Description)}[/]");
        }
    }
}
