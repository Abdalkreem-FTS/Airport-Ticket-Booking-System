using ATBS.Console;
using ATBS.Console.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ATBS.Tests;

public sealed class DiRegistrationTests
{
    [Fact]
    public void All_Application_Services_Resolve_From_The_Container()
    {
        using var provider = new ServiceCollection().AddAppServices().BuildServiceProvider();

        provider.GetRequiredService<IFileTransactionFactory>().Should().NotBeNull();
        provider.GetRequiredService<ITransactionalFileStorage>().Should().NotBeNull();
        provider.GetRequiredService<IBookingService>().Should().NotBeNull();
        provider.GetRequiredService<IFlightService>().Should().NotBeNull();
        provider.GetRequiredService<IManagerBookingService>().Should().NotBeNull();
        provider.GetRequiredService<IFlightImportService>().Should().NotBeNull();
    }
}