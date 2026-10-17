using SevaOne.Business.Stages.Packages;

namespace SevaOne.Business.Stages.Samples;

/// <summary>
/// Pilot service packages used by the Stage Engine spike and its tests.
///
/// ILLUSTRATIVE VALUES: the fee, gateway, roles and SLA days below are placeholders chosen to
/// exercise every engine feature, not real department rules. A real package takes them from the
/// catalogue entry and the department's own notification.
/// </summary>
public static class PilotPackages
{
    /// <summary>
    /// Two stages. Stage 1 collects a fee and needs an officer's approval before stage 2 unlocks.
    /// Stage 2 has no fee, needs a second approval to issue, and the applicant has 30 days to
    /// complete it once it unlocks.
    /// </summary>
    public const string IncomeCertificateJson = """
        {
          "serviceCode": "income-certificate",
          "version": 1,
          "name": "Income Certificate (two-stage pilot)",
          "stages": [
            {
              "id": "application",
              "name": "Application and fee",
              "formId": "income-cert-application",
              "fee": { "amount": 30, "gateway": "paygov" },
              "approval": { "role": "tahsildar", "slaDays": 7 }
            },
            {
              "id": "verification",
              "name": "Document verification and issue",
              "formId": "income-cert-documents",
              "approval": { "role": "revenue-officer", "slaDays": 3 },
              "expiresAfterDays": 30
            }
          ]
        }
        """;

    public static ServicePackage IncomeCertificate() => ServicePackageParser.Parse(IncomeCertificateJson);
}
