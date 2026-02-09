using FluentValidation;
using ManagementApi.DTOs.User;

namespace ManagementApi.Validators.User;

public class RemoveUserFromOrganizationRequestValidator : AbstractValidator<RemoveUserFromOrganizationRequest>
{
    public RemoveUserFromOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationId)
            .GreaterThan(0)
            .WithMessage("OrganizationId must be greater than 0");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(100)
            .WithMessage("UserId must not exceed 100 characters");
    }
}
