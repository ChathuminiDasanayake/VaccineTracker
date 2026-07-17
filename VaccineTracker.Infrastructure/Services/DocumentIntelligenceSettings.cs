namespace VaccineTracker.Infrastructure.Services;

public sealed class DocumentIntelligenceSettings
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public string DefaultModelId { get; init; } = "prebuilt-document";
}
