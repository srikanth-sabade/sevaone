using SevaOne.Business.Common;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Infrastructure.Repositories.Portal;

namespace SevaOne.Business.SevaSindhu.Services;

public sealed class CurrentApplicantResolver : ICurrentApplicantResolver
{
    private readonly ICurrentOperator _operator;
    private readonly IPortalRepository _portal;

    public CurrentApplicantResolver(ICurrentOperator @operator, IPortalRepository portal)
    {
        _operator = @operator;
        _portal = portal;
    }

    public async Task<string> ResolveUserIdAsync(string fallbackUserId, CancellationToken ct)
    {
        if (!_operator.IsAuthenticated)
        {
            // Ordinary citizen apply -- unchanged behaviour, the fixed configured anonymous id.
            return fallbackUserId;
        }

        // An operator is at the counter: they must have bound a citizen via OTP first (see
        // OperatorCitizenBindService + the Catalog 2.0 bind modal) -- an operator session with
        // no bound citizen must never silently fall back to the anonymous id, since that would
        // submit the apply as nobody in particular on a real government API. The bind itself is
        // still required (proves the operator is genuinely assisting someone), but the User_Id
        // sent to Seva Sindhu is the ASSISTING OPERATOR's own whitelist id (operator_whitelist.
        // user_id), not the citizen's -- confirmed directly (an "Invalid Token" from Seva
        // Sindhu traced back to this: the citizen's SevaOneUserId was never a registered Seva
        // Sindhu identity, so the token generated for it was never valid there). Same fix as
        // PortalApplyService's AJSK branch.
        if (_operator.ActiveCitizenId is not { } citizenId)
        {
            throw new DomainException("Bind a citizen before applying on their behalf.");
        }

        _ = await _portal.GetCitizenByIdAsync(citizenId, ct)
            ?? throw new DomainException("The bound citizen could not be found. Please bind again.");

        return _operator.OperatorCode
               ?? throw new DomainException("Not authenticated as an operator.");
    }
}
