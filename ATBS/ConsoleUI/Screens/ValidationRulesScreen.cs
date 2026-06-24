using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

public static class ValidationRulesScreen
{
    public static void Run(AppServices services)
    {
        AppHeader.Render("Flight validation rules", "Dynamic model constraints used during import.");
        var rules = services.ValidationMetadataService.GetFlightValidationRules();
        ValidationTableRenderer.Render(rules);
        PromptHelpers.Pause();
    }
}
