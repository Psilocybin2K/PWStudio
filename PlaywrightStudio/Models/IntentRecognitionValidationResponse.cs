namespace PlaywrightStudio.Models
{
    public record IntentRecognitionValidationResponse(
        string OriginalUserMessage,
        string PlanStatus,
        TaskValidation[] Tasks,
        Assessment Assessment,
        Recommendations Recommendations
    );

    public record TaskValidation(
        int TaskNumber,
        string ToolUsed,
        IDictionary<string, object> ParametersUsed,
        string ValidationStatus,
        string[] IssuesFound,
        string? SuggestedFix
    );

    public record Assessment(
        Completeness Completeness,
        TechnicalAccuracy TechnicalAccuracy,
        IntentFulfillment IntentFulfillment
    );

    public record Completeness(
        string[] MissingTasks,
        string[] UnnecessaryTasks,
        string TaskOrder
    );

    public record TechnicalAccuracy(
        bool ToolSelection,
        bool CssPathValidity,
        bool ParameterAccuracy
    );

    public record IntentFulfillment(
        bool UserGoalAchievement,
        bool EdgeCasesHandled
    );

    public record Recommendations(
        string[] CriticalIssues,
        string[] Improvements,
        string FinalVerdict,
        string Confidence
    );
}

