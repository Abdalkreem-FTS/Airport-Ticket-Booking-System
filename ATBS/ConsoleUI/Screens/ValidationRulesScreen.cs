using ATBS.Abstractions;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Displays the flight validation rules used by the import process.
/// </summary>
public sealed class ValidationRulesScreen(IValidationMetadataService validationMetadataService)
{
    /// <summary>
    /// Shows manager-facing validation metadata.
    /// </summary>
    public void Run()
    {
        AppHeader.Render("Flight validation rules", "Dynamic model constraints used during import.");
        
        var rules = validationMetadataService.GetFlightValidationRules();
        ValidationTableRenderer.Render(rules);
        
        PromptHelpers.Pause();
    }
}
