using ATBS;
using ATBS.ConsoleUI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAppServices(); // Registering of services

using var serviceProvider = services.BuildServiceProvider();
serviceProvider.SeedData();

var app = serviceProvider.GetRequiredService<ConsoleApp>();
app.Run();
