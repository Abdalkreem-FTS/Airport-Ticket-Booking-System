using ATBS.Console.Models;
using ATBS.Console.Transactions.Enums;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.Integration;

/// <summary>
/// Exercises the transaction <i>mechanism</i> directly through the ambient-scope API, against real files:
/// staged writes stay invisible to the on-disk table until commit, rollback (or a dropped scope) discards
/// them, and reads inside a transaction see the transaction's own pending writes.
/// </summary>
public sealed class TransactionIntegrationTests
{
    [Fact]
    public async Task Rollback_DiscardsStagedWrites_LeavingTheFileUnchanged()
    {
        await using var harness = new IntegrationTestHarness();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        using (var scope = harness.TransactionFactory.Begin(IsolationLevel.Serializable))
        {
            await SetEconomySeatsAsync(harness, flight.Id, 1);
            scope.Rollback();
        }

        EconomySeats(await harness.ReloadFlightAsync(flight)).Should().Be(5);
    }

    [Fact]
    public async Task Dispose_WithoutCommit_RollsBack()
    {
        await using var harness = new IntegrationTestHarness();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        // Leaving the scope without committing must behave like a rollback (the crash-safety guarantee).
        using (harness.TransactionFactory.Begin(IsolationLevel.Serializable))
        {
            await SetEconomySeatsAsync(harness, flight.Id, 0);
        }

        EconomySeats(await harness.ReloadFlightAsync(flight)).Should().Be(5);
    }

    [Fact]
    public async Task Commit_PersistsStagedWrites()
    {
        await using var harness = new IntegrationTestHarness();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        using (var scope = harness.TransactionFactory.Begin(IsolationLevel.Serializable))
        {
            await SetEconomySeatsAsync(harness, flight.Id, 1);
            await scope.CommitAsync();
        }

        EconomySeats(await harness.ReloadFlightAsync(flight)).Should().Be(1);
    }

    [Fact]
    public async Task ReadInsideTransaction_SeesTheTransactionsOwnPendingWrite()
    {
        await using var harness = new IntegrationTestHarness();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        using var scope = harness.TransactionFactory.Begin(IsolationLevel.Serializable);
        await SetEconomySeatsAsync(harness, flight.Id, 1);

        // A fresh read within the same transaction reflects the staged (not yet committed) write.
        var rereadWithinTransaction = await harness.FlightRepository.GetByIdAsync(flight.Id);
        EconomySeats(rereadWithinTransaction.Value).Should().Be(1);

        scope.Rollback();
    }

    [Fact]
    public async Task Begin_WhileAnotherTransactionIsActive_Throws()
    {
        await using var harness = new IntegrationTestHarness();

        // No await separates the two Begin calls, so both observe the same ambient (async-local) transaction.
        using var _ = harness.TransactionFactory.Begin(IsolationLevel.Serializable);

        var act = () => harness.TransactionFactory.Begin(IsolationLevel.Serializable);
        act.Should().Throw<InvalidOperationException>();
    }

    private static async Task SetEconomySeatsAsync(IntegrationTestHarness harness, Guid flightId, int seats)
    {
        var flight = (await harness.FlightRepository.GetByIdAsync(flightId)).Value;
        flight.ClassPrices.Single().AvailableSeats = seats;
        var update = await harness.FlightRepository.UpdateAsync(flight);
        update.IsSuccess.Should().BeTrue();
    }

    private static int EconomySeats(Flight flight) => flight.ClassPrices.Single().AvailableSeats;
}
