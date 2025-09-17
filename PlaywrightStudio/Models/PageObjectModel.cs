namespace PlaywrightStudio.Models
{
    public record PageObjectModel(
        string Type,
        string Url,
        string Name,
        string Description,
        PageElement[] Elements,
        PageTask[] Tasks,
        string[] Utterances
    );

    public record PageElement(
        string Name,
        string CssPath,
        string[] Utterances
    );

    public record PageElementParameter(
        string Name,
        string Type
    );

    public record PageTask(
        string Name,
        string Description,
        PageElementParameter[]? Parameters,
        PageStep[] Steps,
        string[] Utterances
    );

    public record PageStep(
        string Description,
        string Action,
        string Element,
        string Value,
        string[] Utterances
    );
}
