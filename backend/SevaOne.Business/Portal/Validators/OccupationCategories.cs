namespace SevaOne.Business.Portal.Validators;

/// <summary>Occupation codes the profile page offers; the frontend dropdown (src/lib/occupations.ts) must use the same codes.</summary>
public static class OccupationCategories
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "student", "farmer", "agri_labourer", "fisherman", "construction_worker", "unorganised_worker",
        "artisan", "driver", "govt_employee", "private_employee", "self_employed", "homemaker",
        "retired", "unemployed", "other",
    };

    /// <summary>Blank is fine (occupation is optional); anything else must be a known code.</summary>
    public static bool IsValid(string? code) => string.IsNullOrWhiteSpace(code) || Codes.Contains(code);
}
