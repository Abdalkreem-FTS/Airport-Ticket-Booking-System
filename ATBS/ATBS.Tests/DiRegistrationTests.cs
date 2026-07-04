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

        Assert.NotNull(provider.GetRequiredService<IFileTransactionFactory>());
        Assert.NotNull(provider.GetRequiredService<ITransactionalFileStorage>());
        Assert.NotNull(provider.GetRequiredService<IBookingService>());
        Assert.NotNull(provider.GetRequiredService<IFlightService>());
        Assert.NotNull(provider.GetRequiredService<IManagerBookingService>());
        Assert.NotNull(provider.GetRequiredService<IFlightImportService>());
    }
}