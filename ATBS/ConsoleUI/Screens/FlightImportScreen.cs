using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Previews CSV flight imports, shows validation errors, and saves valid rows.
/// </summary>
public static class FlightImportScreen
{
    /// <summary>
    /// Runs the manager CSV import workflow.
    /// </summary>
    public static void Run(AppServices services)
    {
        AppHeader.Render("Import flights from CSV");
        var path = PromptHelpers.OptionalText("CSV file path:");
        if (path is null)
        {
            return;
        }

        var preview = AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Reading CSV...", _ => services.FlightImportService.PreviewImport(path));

        AppHeader.Render("Import flights from CSV", "Preview result");
        RenderSummary(preview.TotalRows, preview.ValidRows, preview.FailedRows);

        if (preview.HasErrors)
        {
            ErrorTableRenderer.Render(preview.Errors);
        }

        if (preview.ValidRows == 0)
        {
            PromptHelpers.Pause();
            
            return;
        }

        var shouldImport = AnsiConsole.Confirm($"Import [green]{preview.ValidRows}[/] valid flight row(s)?");
        if (!shouldImport)
        {
            return;
        }

        var result = AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Importing flights...", _ => services.FlightImportService.Import(path));

        AnsiConsole.MarkupLine($"[green]Imported {result.ValidRows} flight(s).[/]");
        PromptHelpers.Pause();
    }

    private static void RenderSummary(int totalRows, int validRows, int failedRows)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn("Name")
            .AddColumn("Value");

        table.AddRow("[grey]Total rows[/]", totalRows.ToString());
        table.AddRow("[grey]Valid rows[/]", $"[green]{validRows}[/]");
        table.AddRow("[grey]Failed rows[/]", failedRows == 0 ? "0" : $"[red]{failedRows}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
