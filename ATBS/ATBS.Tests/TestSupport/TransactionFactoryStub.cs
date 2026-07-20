using ATBS.Console.Abstractions;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;
using Moq;

namespace ATBS.Tests.TestSupport;

/// <summary>
/// Helpers for turning an <see cref="IFileTransactionFactory"/> mock into a transparent pass-through.
/// The real factory acquires locks, retries conflicts, and persists a write-ahead log; none of that is the
/// subject of these tests, so we simply run the unit of work and hand back its result. This lets the service
/// logic (seat maths, ownership checks, error mapping) be tested in isolation from the transaction machinery.
/// </summary>
internal static class TransactionFactoryStub
{
    /// <summary>
    /// Configures <c>ExecuteAsync&lt;T&gt;</c> to invoke the supplied work once and return its result directly.
    /// Call once per closed result type the service under test executes (e.g. <c>Booking</c>, <c>Updated</c>).
    /// </summary>
    public static Mock<IFileTransactionFactory> RunsWorkInline<T>(this Mock<IFileTransactionFactory> factory)
    {
        factory.Setup(f => f.ExecuteAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<Func<Task<Result<T>>>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            // Return the work's own Task so the awaited result flows straight back to the service, with none
            // of the locking/retry machinery in between. The delegate only awaits mocked dependencies.
            .Returns((IsolationLevel _, Func<Task<Result<T>>> work, int _, CancellationToken _) => work());

        return factory;
    }
}
