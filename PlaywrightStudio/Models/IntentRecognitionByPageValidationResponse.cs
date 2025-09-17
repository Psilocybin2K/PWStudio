namespace PlaywrightStudio.Models
{
    public record IntentRecognitionByPageValidationResponse(
        string OriginalUserMessage,
        string AnalysisStatus,
        ElementValidation[] ElementValidations,
        PageTaskValidation[] TaskValidations,
        PageAssessment Assessment,
        PageRecommendations Recommendations
    );

    public record ElementValidation(
        string ElementName,
        string CssPath,
        string RelevanceAssessment,
        string[] IssuesFound,
        string? SuggestedFix
    );

    public record PageTaskValidation(
        string TaskName,
        string RelevanceAssessment,
        string ParameterAssessment,
        string[] IssuesFound,
        string? SuggestedFix
    );

    public record PageAssessment(
        PageCompleteness Completeness,
        PageAccuracy Accuracy,
        PageIntentAlignment IntentAlignment
    );

    public record PageCompleteness(
        string[] MissingElements,
        string[] MissingTasks,
        string[] OverAnalyzed
    );

    public record PageAccuracy(
        bool ElementRelevanceAccuracy,
        bool TaskRelevanceAccuracy,
        bool KeywordMatchingAccuracy
    );

    public record PageIntentAlignment(
        bool UserIntentUnderstanding,
        bool PageContextUtilization
    );

    public record PageRecommendations(
        string[] CriticalIssues,
        string[] Improvements,
        string FinalVerdict,
        string Confidence
    );
}
