using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.GetInfo;

public sealed record GetOrgInfoQuery(Guid OrganizationId) : IRequest<OrgInfoResponse>;

public sealed record OrgInfoResponse(string Name, string TaxNumber, string RegistrationNumber);

public sealed class GetOrgInfoQueryHandler : IRequestHandler<GetOrgInfoQuery, OrgInfoResponse>
{
    private readonly IOrganizationRepository _repo;

    public GetOrgInfoQueryHandler(IOrganizationRepository repo) => _repo = repo;

    public async Task<OrgInfoResponse> Handle(GetOrgInfoQuery request, CancellationToken cancellationToken)
    {
        var org = await _repo.GetByIdAsync(new OrganizationId(request.OrganizationId), cancellationToken)
            ?? throw new InvalidOperationException("Organization not found.");
        return new OrgInfoResponse(org.Name, org.TaxNumber, org.RegistrationNumber);
    }
}
