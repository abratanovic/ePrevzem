using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.ListMembers;

public sealed record ListOrganisationMembersQuery(Guid OrganizationId) : IRequest<IReadOnlyList<MemberDto>>;

public sealed record MemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset? LastLoginAt);

public sealed class ListOrganisationMembersQueryHandler
    : IRequestHandler<ListOrganisationMembersQuery, IReadOnlyList<MemberDto>>
{
    private readonly IEmployeeAccountRepository _repo;

    public ListOrganisationMembersQueryHandler(IEmployeeAccountRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<MemberDto>> Handle(
        ListOrganisationMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _repo.GetByOrganisationIdAsync(
            new OrganizationId(request.OrganizationId), cancellationToken);

        return members
            .Select(m => new MemberDto(
                m.Id.Value,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Status.ToString(),
                m.Roles.Select(r => r.ToString()).ToList(),
                m.LastLoginAt))
            .ToList();
    }
}
