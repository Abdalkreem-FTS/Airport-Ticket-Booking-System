using ATBS;
using ATBS.ConsoleUI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAppServices(); // Registering of services

using var serviceProvider = services.BuildServiceProvider();
await serviceProvider.SeedDataAsync();

var app = serviceProvider.GetRequiredService<ConsoleApp>();
await app.RunAsync();
