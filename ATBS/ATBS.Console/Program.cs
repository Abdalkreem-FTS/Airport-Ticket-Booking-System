using ATBS;
using ATBS.Console;
using ATBS.Console.ConsoleUI;
using ATBS.Console.Transactions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAppServices(); // Registering of services

await using var serviceProvider = services.BuildServiceProvider();

// Finish or roll back any transactions interrupted by a previous crash before serving requests.
var transactionLogDirectory = serviceProvider.GetRequiredService<TransactionLogDirectory>();
await TransactionRecovery.RecoverAll(transactionLogDirectory.DirectoryPath);

await serviceProvider.SeedDataAsync();

var app = serviceProvider.GetRequiredService<ConsoleApp>();
await app.RunAsync();