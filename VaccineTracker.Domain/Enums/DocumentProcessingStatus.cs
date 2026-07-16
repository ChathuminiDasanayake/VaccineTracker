namespace VaccineTracker.Domain.Enums;

public enum DocumentProcessingStatus
{
    Uploaded = 1,
    Processing = 2,
    ReviewRequired = 3,
    Approved = 4,
    Rejected = 5,
    Failed = 6
}
