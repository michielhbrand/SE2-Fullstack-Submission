using FluentValidation;
using ManagementApi.DTOs.Auth;

namespace ManagementApi.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username or email is required")
            .MinimumLength(3)
            .WithMessage("Username or email must be at least 3 characters long")
            .MaximumLength(100)
            .WithMessage("Username or email must not exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long");
    }
}
