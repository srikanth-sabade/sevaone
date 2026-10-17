using SevaOne.Domain.Models;

namespace SevaOne.Business.Common;

/// <summary>Computes Citizen.ProfileFinishPct — the 0-100 shown to the citizen right after login/register. Pure/stateless so both login and profile-update flows can call it after mutating a citizen, without duplicating the field list.</summary>
public static class ProfileCompleteness
{
    public static int Calculate(Citizen citizen)
    {
        var fields = new[]
        {
            !string.IsNullOrWhiteSpace(citizen.Name),
            !string.IsNullOrWhiteSpace(citizen.Mobile),
            !string.IsNullOrWhiteSpace(citizen.Email),
            !string.IsNullOrWhiteSpace(citizen.Address),
            !string.IsNullOrWhiteSpace(citizen.District),
            !string.IsNullOrWhiteSpace(citizen.Taluk),
            !string.IsNullOrWhiteSpace(citizen.StateName),
            !string.IsNullOrWhiteSpace(citizen.VillageName),
            citizen.DateOfBirth is not null,
            !string.IsNullOrWhiteSpace(citizen.Gender),
            !string.IsNullOrWhiteSpace(citizen.AadhaarMasked)
        };

        var filled = fields.Count(f => f);
        return (int)Math.Round(filled * 100.0 / fields.Length);
    }
}
