using AutoMapper;

namespace SevaOne.Business.Orchestration.Mappings;

/// <summary>No entity-to-DTO maps yet — DepartmentSessionDto and ApplicationDetailDto (via SubmitApplicationService) are both built from more than one source (connector result + flags, or delegated to Status's own mapping), so they stay hand-constructed rather than forced through AutoMapper.</summary>
public sealed class OrchestrationProfile : Profile
{
}
