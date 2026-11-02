using AutoMapper;
using SevaOne.Business.Common;
using SevaOne.Business.Status.DTOs;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Orchestration;

namespace SevaOne.Business.Status.Mappings;

public sealed class StatusProfile : Profile
{
    public StatusProfile()
    {
        // All three destinations are positional records — ForCtorParam feeds the constructor
        // directly, unlike ForMember which assumes a settable property AutoMapper can assign
        // after construction.
        CreateMap<ApplicationWithNames, ApplicationSummaryDto>()
            .ForCtorParam("Status", o => o.MapFrom(s => ApplicationStatusMapper.ToApi(s.Status)));

        CreateMap<ApplicationWithNames, TrackDto>()
            .ForCtorParam("Status", o => o.MapFrom(s => ApplicationStatusMapper.ToApi(s.Status)));

        CreateMap<ApplicationEvent, TimelineDto>()
            .ForCtorParam("Status", o => o.MapFrom(s => ApplicationStatusMapper.ToApi(s.Status)));
    }
}
