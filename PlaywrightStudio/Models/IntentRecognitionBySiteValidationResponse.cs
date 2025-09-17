namespace PlaywrightStudio.Models
{
    public record IntentRecognitionBySiteValidationResponse(
        string OriginalUserMessage,
        string AnalysisStatus,
        SitePageValidation[] PageValidations,
        CrossPageAssessment CrossPageAssessment,
        SiteAssessment Assessment,
        SiteRecommendations Recommendations
    );

    public record SitePageValidation(
        string PageName,
        string PageUrl,
        string RelevanceAssessment,
        string ElementAssessment,
        string TaskAssessment,
        string[] IssuesFound,
        string? SuggestedFix
    );

    public record CrossPageAssessment(
        string NavigationFlowCorrectness,
        string[] MissingPageConnections,
        string[] UnnecessaryPageConnections
    );

    public record SiteAssessment(
        SiteCompleteness Completeness,
        SiteAccuracy Accuracy,
        SiteIntentAlignment IntentAlignment
    );

    public record SiteCompleteness(
        string[] MissingPages,
        string[] MissingElements,
        string[] MissingTasks,
        string[] OverAnalyzed
    );

    public record SiteAccuracy(
        bool PageRelevanceAccuracy,
        bool ElementRelevanceAccuracy,
        bool TaskRelevanceAccuracy,
        bool KeywordMatchingAccuracy
    );

    public record SiteIntentAlignment(
        bool UserIntentUnderstanding,
        bool SiteContextUtilization,
        bool CrossPageFlowUnderstanding
    );

    public record SiteRecommendations(
        string[] CriticalIssues,
        string[] Improvements,
        string FinalVerdict,
        string Confidence
    );
}
