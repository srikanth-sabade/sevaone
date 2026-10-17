using AutoMapper;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Domain.Models;

namespace SevaOne.Business.Portal.Mappings;

public sealed class PortalProfile : Profile
{
    public PortalProfile()
    {
        // AadhaarVerified has no matching property on Citizen -- it's derived from AadhaarHash
        // being set at all (only ever true when sourced from a real DigiLocker Aadhaar document,
        // see CitizenSessionService.EstablishAsync), same ForCtorParam pattern as ReturnUrl below.
        CreateMap<Citizen, MeDto>()
            .ForCtorParam("AadhaarVerified", o => o.MapFrom(s => s.AadhaarHash != null));

        // UserDetailsResult is a positional record — ForCtorParam feeds the constructor directly,
        // unlike ForMember which assumes a settable property AutoMapper can assign after construction.
        // ServiceId/AssistedMode/OperatorId/ExpiresAt/ReturnUrl have no source on Citizen at all --
        // they come from the token, not the citizen (see PortalDeptFacingService.GetUserDetailsAsync,
        // which overwrites all five via `with` right after this Map call) -- but AutoMapper's
        // config validation still requires every ctor param to resolve to *something*, so they're
        // mapped to literal defaults here.
        CreateMap<Citizen, UserDetailsResult>()
            .ForCtorParam("DistrictName", o => o.MapFrom(s => s.District))
            .ForCtorParam("ServiceId", o => o.MapFrom(_ => string.Empty))
            .ForCtorParam("AssistedMode", o => o.MapFrom(_ => false))
            .ForCtorParam("OperatorId", o => o.MapFrom(_ => (string?)null))
            .ForCtorParam("ExpiresAt", o => o.MapFrom(_ => default(DateTimeOffset)))
            .ForCtorParam("ReturnUrl", o => o.MapFrom(_ => (string?)null));
    }
}
