using Microsoft.AspNetCore.Html;

namespace Auth.Models;

public record FieldModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Type { get; init; }
    public string? Icon { get; init; }
    public string? Value { get; init; }
    public string? Placeholder { get; init; }
    public string? Error { get; init; }
    public string? AutoComplete { get; init; }
    public IHtmlContent? LabelRight { get; init; }
}

public record PasswordFieldModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Value { get; init; }
    public string? Placeholder { get; init; }
    public string? Error { get; init; }
    public bool ShowStrength { get; init; }
    public string? AutoComplete { get; init; }
    public IHtmlContent? LabelRight { get; init; }
    public bool Show { get; init; }
}

public record CheckboxModel
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public bool Checked { get; init; }
}

public record BrandPanelModel
{
    public required string Headline { get; init; }
    public required string Sub { get; init; }
}
