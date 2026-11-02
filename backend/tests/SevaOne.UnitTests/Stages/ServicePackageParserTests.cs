using FluentAssertions;
using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Samples;

namespace SevaOne.UnitTests.Stages;

public class ServicePackageParserTests
{
    [Fact]
    public void Pilot_package_parses_with_its_two_stages()
    {
        var package = ServicePackageParser.Parse(PilotPackages.IncomeCertificateJson);

        package.ServiceCode.Should().Be("income-certificate");
        package.Stages.Should().HaveCount(2);

        var first = package.Stages[0];
        first.Id.Should().Be("application");
        first.Fee.Should().Be(new FeeRule(30m, "paygov"));
        first.Approval.Should().Be(new ApprovalRule("tahsildar", 7));
        first.ExpiresAfterDays.Should().BeNull();

        var second = package.Stages[1];
        second.Fee.Should().BeNull();
        second.Approval!.Role.Should().Be("revenue-officer");
        second.ExpiresAfterDays.Should().Be(30);
    }

    [Fact]
    public void Duplicate_stage_ids_are_rejected()
    {
        var json = PilotPackages.IncomeCertificateJson.Replace("\"verification\"", "\"application\"");

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("duplicated"));
    }

    [Fact]
    public void A_fee_must_be_positive()
    {
        var json = PilotPackages.IncomeCertificateJson.Replace("\"amount\": 30", "\"amount\": 0");

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("fee.amount"));
    }

    [Fact]
    public void A_misspelled_property_fails_instead_of_silently_dropping_the_approval_gate()
    {
        var json = PilotPackages.IncomeCertificateJson.Replace("\"approval\": { \"role\": \"tahsildar\"", "\"aproval\": { \"role\": \"tahsildar\"");

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("Malformed"));
    }

    [Fact]
    public void A_package_needs_at_least_one_stage()
    {
        const string json = """{ "serviceCode": "x", "version": 1, "name": "X", "stages": [] }""";

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("at least one stage"));
    }

    [Fact]
    public void Stage_ids_are_restricted_to_a_safe_alphabet()
    {
        var json = PilotPackages.IncomeCertificateJson.Replace("\"application\"", "\"Application Form!\"");

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("lowercase letters"));
    }

    [Fact]
    public void An_approval_needs_a_role_and_a_positive_sla()
    {
        var json = PilotPackages.IncomeCertificateJson.Replace("\"slaDays\": 7", "\"slaDays\": 0");

        var act = () => ServicePackageParser.Parse(json);

        act.Should().Throw<PackageValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("slaDays"));
    }
}
