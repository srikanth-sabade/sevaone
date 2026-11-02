using SevaOne.Domain.Enums;

namespace SevaOne.Business.Common;

public static class ApplicationStatusMapper
{
    public static ApplicationStatus FromDepartment(string status) => status.Trim().ToLowerInvariant() switch
    {
        "submitted" => ApplicationStatus.Submitted,
        "documentverification" or "document_verification" or "document verification" => ApplicationStatus.DocumentVerification,
        "underreview" or "under_review" or "under review" => ApplicationStatus.UnderReview,
        "approved" => ApplicationStatus.Approved,
        "rejected" => ApplicationStatus.Rejected,
        _ => ApplicationStatus.Submitted
    };

    public static string ToApi(ApplicationStatus status) => status switch
    {
        ApplicationStatus.DocumentVerification => "DocumentVerification",
        ApplicationStatus.UnderReview => "UnderReview",
        _ => status.ToString()
    };

    public static string ToApi(int status) => ToApi((ApplicationStatus)status);
}
