using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Displays the flight validation rules used by the import process.
/// </summary>
public static class ValidationRulesScreen
{
    /// <summary>
    /// Shows manager-facing validation metadata.
    /// </summary>
    public static void Run(AppServices services)
    {
        AppHeader.Render("Flight validation rules", "Dynamic model constraints used during import.");
        
        var rules = services.ValidationMetadataService.GetFlightValidationRules();
        ValidationTableRenderer.Render(rules);
        
        PromptHelpers.Pause();
    }
}
