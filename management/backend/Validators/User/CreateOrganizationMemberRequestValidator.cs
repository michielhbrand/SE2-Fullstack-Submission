using FluentValidation;
using ManagementApi.DTOs.User;

namespace ManagementApi.Validators.User;

public class CreateOrganizationMemberRequestValidator : AbstractValidator<CreateOrganizationMemberRequest>
{
    public CreateOrganizationMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid UserRole");
    }
}
