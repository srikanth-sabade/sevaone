using System.Text.Json;
using System.Text.RegularExpressions;

namespace SevaOne.Business.Stages.Packages;

/// <summary>Thrown when a package is malformed or breaks a rule the engine relies on.</summary>
public sealed class PackageValidationException : Exception
{
    public PackageValidationException(IReadOnlyList<string> errors)
        : base("Invalid service package: " + string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Parses and validates package JSON. Unknown properties are rejected on purpose: a typo such as
/// "aproval" in designer output must fail loudly instead of silently producing a stage with no
/// approval gate.
/// </summary>
public static class ServicePackageParser
{
    // Stage ids end up inside fee references, so keep them to a boring, URL-safe alphabet.
    private static readonly Regex StageIdPattern = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public static ServicePackage Parse(string json)
    {
        ServicePackage? package;
        try
        {
            package = JsonSerializer.Deserialize<ServicePackage>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new PackageValidationException([$"Malformed package JSON: {ex.Message}"]);
        }

        if (package is null)
        {
            throw new PackageValidationException(["Package JSON is empty."]);
        }

        var errors = Validate(package);
        if (errors.Count > 0)
        {
            throw new PackageValidationException(errors);
        }

        return package;
    }

    public static IReadOnlyList<string> Validate(ServicePackage package)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(package.ServiceCode))
        {
            errors.Add("serviceCode is required.");
        }

        if (package.Version < 1)
        {
            errors.Add("version must be 1 or higher.");
        }

        if (string.IsNullOrWhiteSpace(package.Name))
        {
            errors.Add("name is required.");
        }

        if (package.Stages is null || package.Stages.Count == 0)
        {
            errors.Add("at least one stage is required.");
            return errors;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < package.Stages.Count; i++)
        {
            var stage = package.Stages[i];
            var at = $"stages[{i}]";
            if (stage is null)
            {
                errors.Add($"{at} is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(stage.Id))
            {
                errors.Add($"{at}.id is required.");
            }
            else if (!StageIdPattern.IsMatch(stage.Id))
            {
                errors.Add($"{at}.id '{stage.Id}' may only use lowercase letters, digits and '-'.");
            }
            else if (!seen.Add(stage.Id))
            {
                errors.Add($"{at}.id '{stage.Id}' is duplicated.");
            }

            if (string.IsNullOrWhiteSpace(stage.Name))
            {
                errors.Add($"{at}.name is required.");
            }

            if (stage.Fee is { } fee)
            {
                if (fee.Amount <= 0)
                {
                    errors.Add($"{at}.fee.amount must be greater than zero.");
                }

                if (string.IsNullOrWhiteSpace(fee.Gateway))
                {
                    errors.Add($"{at}.fee.gateway is required.");
                }
            }

            if (stage.Approval is { } approval)
            {
                if (string.IsNullOrWhiteSpace(approval.Role))
                {
                    errors.Add($"{at}.approval.role is required.");
                }

                if (approval.SlaDays < 1)
                {
                    errors.Add($"{at}.approval.slaDays must be 1 or higher.");
                }
            }

            if (stage.ExpiresAfterDays is < 1)
            {
                errors.Add($"{at}.expiresAfterDays must be 1 or higher.");
            }
        }

        return errors;
    }
}
