using ATBS.Composition;
using ATBS.ConsoleUI;

var services = AppServices.Create();

var app = new ConsoleApp(services);

app.Run();
