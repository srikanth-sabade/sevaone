namespace SevaOne.Business.Status.DTOs;

public sealed record TimelineDto(string Status, string Note, DateTimeOffset At);

public sealed record ApplicationSummaryDto(
    string SevaReference,
    string? DepartmentReference,
    string ServiceCode,
    string ServiceName,
    string DepartmentCode,
    string DepartmentName,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset SakalaDueAt);

public sealed record ApplicationDetailDto(
    string SevaReference,
    string? DepartmentReference,
    string ServiceCode,
    string ServiceName,
    string DepartmentCode,
    string DepartmentName,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset SakalaDueAt,
    string ApplicantName,
    string Address,
    string? Purpose,
    string? District,
    string? Taluk,
    IReadOnlyList<TimelineDto> Timeline);

public sealed record GetApplicationRequest(string Reference);

public sealed record TrackApplicationRequest(string Reference);

public sealed record TrackDto(
    string SevaReference,
    string ServiceName,
    string DepartmentName,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset SakalaDueAt);

/// <summary>One escalation/complaint slot within a SakalaTrackDto -- null (the whole record, not
/// just fields within it) when that complaint was never filed.</summary>
public sealed record SakalaComplaintDto(
    string ComplaintNo,
    DateOnly? ComplaintDate,
    DateOnly? ClosedDate,
    DateOnly? DueDate,
    string? StatusCode,
    string? StatusDescription,
    int? DaysPending);

/// <summary>A real Sakala government record, looked up live by GSC number -- see ISakalaClient.
/// Independent of any SevaOne-owned application; TrackResultDto.Source distinguishes this from
/// TrackDto (SevaOne's own record) so the frontend renders the right card for each.</summary>
public sealed record SakalaTrackDto(
    string GscNo,
    string ApplicantName,
    string ServiceName,
    string InstitutionName,
    string? OfficerName,
    DateOnly? SakalaDate,
    DateOnly? SakalaClosedDate,
    DateOnly? DueDate,
    int? DaysPending,
    string StatusCode,
    string StatusDescription,
    SakalaComplaintDto? FirstComplaint,
    SakalaComplaintDto? SecondComplaint);

/// <summary>Wraps whichever source resolved the citizen's tracking-number lookup -- SevaOne's own
/// application record if the reference matches one, otherwise a live Sakala GSC lookup as a
/// fallback (see ApplicationStatusService.TrackAsync). Exactly one of SevaOne/Sakala is
/// non-null, discriminated by Source.</summary>
public sealed record TrackResultDto(string Source, TrackDto? SevaOne, SakalaTrackDto? Sakala);

public sealed record ApplyDepartmentStatusRequest(
    string DepartmentCode,
    string DepartmentReference,
    string Status,
    string Note);
