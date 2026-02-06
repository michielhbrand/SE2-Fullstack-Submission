using FluentValidation;
using ManagementApi.DTOs.Auth;

namespace ManagementApi.Validators.Auth;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
