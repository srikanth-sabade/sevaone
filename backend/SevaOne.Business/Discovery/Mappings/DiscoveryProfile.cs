using AutoMapper;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Infrastructure.Repositories.Discovery;

namespace SevaOne.Business.Discovery.Mappings;

public sealed class DiscoveryProfile : Profile
{
    public DiscoveryProfile()
    {
        CreateMap<DepartmentWithSchemeCount, DepartmentDto>();

        // SchemeSummaryDto/SchemeDetailDto are positional records built via constructor mapping —
        // ForMember configures a post-construction property set, which AutoMapper can't combine
        // with constructor mapping for init-only record properties. ForCtorParam is the record-safe
        // equivalent: it feeds the named constructor parameter directly.
        CreateMap<SchemeWithDepartment, SchemeSummaryDto>()
            .ForCtorParam("Eligibility", o => o.MapFrom(s => JsonLists.Read(s.EligibilityJson)))
            .ForCtorParam("Aliases", o => o.MapFrom(s => JsonLists.Read(s.AliasesJson)));

        CreateMap<SchemeWithDepartment, SchemeDetailDto>()
            .ForCtorParam("Documents", o => o.MapFrom(s => JsonLists.Read(s.RequiredDocumentsJson)))
            .ForCtorParam("Eligibility", o => o.MapFrom(s => JsonLists.Read(s.EligibilityJson)))
            .ForCtorParam("Aliases", o => o.MapFrom(s => JsonLists.Read(s.AliasesJson)))
            .ForCtorParam("Steps", o => o.MapFrom(s => JsonLists.Read(s.StepsJson)));
    }
}
