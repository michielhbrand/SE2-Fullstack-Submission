using FluentValidation;
using ManagementApi.DTOs.User;

namespace ManagementApi.Validators.User;

public class GetOrganizationMembersRequestValidator : AbstractValidator<GetOrganizationMembersRequest>
{
    public GetOrganizationMembersRequestValidator()
    {
        RuleFor(x => x.OrganizationId)
            .GreaterThan(0)
            .WithMessage("OrganizationId must be greater than 0");
    }
}
