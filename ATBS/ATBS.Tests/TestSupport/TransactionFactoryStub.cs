using ATBS.Console.Abstractions;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;
using NSubstitute;

namespace ATBS.Tests.TestSupport;

/// <summary>
/// Helpers for turning an <see cref="IFileTransactionFactory"/> substitute into a transparent pass-through.
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
    public static IFileTransactionFactory RunsWorkInline<T>(this IFileTransactionFactory factory)
    {
        factory
            .ExecuteAsync(Arg.Any<IsolationLevel>(), Arg.Any<Func<Task<Result<T>>>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            // The work delegate only awaits mocked dependencies, which complete synchronously, so blocking
            // here cannot deadlock. NSubstitute re-wraps the returned Result in a completed Task.
            .Returns(call => call.Arg<Func<Task<Result<T>>>>().Invoke().GetAwaiter().GetResult());

        return factory;
    }
}
