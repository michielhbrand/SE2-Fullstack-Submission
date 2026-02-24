using FluentValidation;
using ManagementApi.DTOs.User;
using Shared.Database.Models;

namespace ManagementApi.Validators.User;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid UserRole value (OrgUser or OrgAdmin)")
            .When(x => x.Role.HasValue);
    }
}
