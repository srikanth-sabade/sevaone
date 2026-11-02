using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>sevaone_service_master. Business unique key is (DepartmentId, ServiceCode) — "DEPARTMENT_ID + SERVICE_ID" — not <see cref="ServiceCode"/> alone, since two departments may reuse a scheme code.</summary>
public sealed class Scheme : BaseModel
{
    public string ServiceCode { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }

    /// <summary>Nullable -- every service belongs to exactly one, real or ungrouped (null). Added in V23, not wired into this model until the Service Catalogue work.</summary>
    public Guid? SubDepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? NameKannada { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SakalaDays { get; set; }

    /// <summary>Whether this service is actually Sakala-enabled, distinct from <see cref="SakalaDays"/> -- a source spreadsheet can carry a days figure without the service being enabled.</summary>
    public bool? IsSakalaEnabled { get; set; }

    /// <summary>The source's raw label ("Non-Sakala", "non sakala", "Yes", ...), preserved for audit even after <see cref="IsSakalaEnabled"/> normalizes it.</summary>
    public string? SakalaLabelRaw { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? DescriptionKannada { get; set; }
    public string? ShortDescription { get; set; }
    public string? ShortDescriptionKannada { get; set; }

    /// <summary>The department's own statutory fee. The legacy free-text fee column this superseded was dropped once nothing read it any more.</summary>
    public decimal? DepartmentFee { get; set; }

    /// <summary>SevaOne's/portal's own service charge, on top of <see cref="DepartmentFee"/>.</summary>
    public decimal? ServiceCharge { get; set; }

    /// <summary>The department's own internal code for this service -- separate from <see cref="ServiceCode"/>, which is SevaOne's own business code.</summary>
    public string? DepartmentServiceId { get; set; }

    public string? Jurisdiction { get; set; }

    /// <summary>'Online' / 'Offline' / 'Both' -- CHECK-constrained at the DB. A source value like "Online/Offline" must be normalized to 'Both' before an upsert, not passed through as-is.</summary>
    public string? ModeOfService { get; set; }

    public string? Remarks { get; set; }

    /// <summary>Required-document checklist (e.g. "Aadhaar Card") — kept for existing catalog seed/read paths. New AI-facing document URLs live in <see cref="SevaOneServiceDocument"/>; that table is authoritative going forward.</summary>
    public string RequiredDocumentsJson { get; set; } = "[]";

    /// <summary>Checklist-style documents have no URL at all -- a different concept from SevaOneServiceDocument's per-row document_url, which is NOT NULL by design.</summary>
    public string? DocumentsJsonKannada { get; set; } = "[]";
    public string EligibilityJson { get; set; } = "[]";
    public string? EligibilityJsonKannada { get; set; } = "[]";
    public string AliasesJson { get; set; } = "[]";
    public string StepsJson { get; set; } = "[]";
    public string? StepsJsonKannada { get; set; } = "[]";

    /// <summary>Legacy free-form portal note. Superseded by <see cref="ServiceUrl"/>/<see cref="DeeplinkCitizenUrl"/>; kept for existing reads.</summary>
    public string? Portal { get; set; }

    /// <summary>Info/detail landing page — what the AI chat reads before asking clarifying questions.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Department's citizen-facing apply entry point. The BFF appends DepartmentID/ServiceID/SevaOneUserID/Token as query params before redirecting here.</summary>
    public string? DeeplinkCitizenUrl { get; set; }

    /// <summary>Department's operator-assisted apply entry point (walk-in citizens applying through a SevaOne help-desk operator). Same query params as <see cref="DeeplinkCitizenUrl"/>, plus AssistedMode/OperatorID. Falls back to <see cref="DeeplinkCitizenUrl"/> if blank.</summary>
    public string? DeeplinkOperatorUrl { get; set; }

    /// <summary>'ajsk' / 'sakala_token' / null. Which external redirect mechanism OPERATOR-assisted apply should use instead of the normal <see cref="DeeplinkOperatorUrl"/>/<see cref="DeeplinkCitizenUrl"/> token flow -- see PortalApplyService.MintApplyTokenAsync. Null (the default) means operator apply behaves exactly like citizen apply. Citizen apply never reads this field.</summary>
    public string? IntegrationMethod { get; set; }

    /// <summary>Seva Sindhu's OWN service_id (a different numbering space from <see cref="DepartmentServiceId"/>), required by the Sakala Token Generation API when <see cref="IntegrationMethod"/> is 'sakala_token'. Null whenever IntegrationMethod isn't 'sakala_token'.</summary>
    public string? SevasindhuServiceId { get; set; }

    /// <summary>Plain fallback when the department has no deep-link integration yet for the relevant audience — the citizen/operator is sent here with no token, since there's no endpoint to redeem one.</summary>
    public string? HomepageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Bumped on every <c>sp_upsert_sevaone_service</c> conflict -- pairs with <see cref="ServiceChangeLog"/>.</summary>
    public int Version { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; }
}
