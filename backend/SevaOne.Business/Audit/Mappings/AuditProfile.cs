using AutoMapper;

namespace SevaOne.Business.Audit.Mappings;

/// <summary>No dedicated Audit business DTOs/service exist yet — audit writes go directly through IAuditRepository from the services that need them.</summary>
public sealed class AuditProfile : Profile
{
}
