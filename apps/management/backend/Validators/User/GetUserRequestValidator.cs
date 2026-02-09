using FluentValidation;
using ManagementApi.DTOs.User;

namespace ManagementApi.Validators.User;

public class GetUserRequestValidator : AbstractValidator<GetUserRequest>
{
    public GetUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .MaximumLength(100)
            .WithMessage("UserId must not exceed 100 characters");
    }
}
