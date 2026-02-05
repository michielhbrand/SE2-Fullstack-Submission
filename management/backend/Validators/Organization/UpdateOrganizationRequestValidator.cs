using FluentValidation;
using ManagementApi.DTOs.Organization;

namespace ManagementApi.Validators.Organization;

public class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2)
            .WithMessage("Organization name must be at least 2 characters long")
            .MaximumLength(200)
            .WithMessage("Organization name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50)
            .WithMessage("Tax number must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(50)
            .WithMessage("Registration number must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.RegistrationNumber));

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Invalid email address format")
            .MaximumLength(100)
            .WithMessage("Email must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .WithMessage("Phone number must not exceed 20 characters")
            .Matches(@"^[\d\s\-\+\(\)]+$")
            .WithMessage("Phone number contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Website)
            .MaximumLength(200)
            .WithMessage("Website URL must not exceed 200 characters")
            .Must(BeAValidUrl)
            .WithMessage("Invalid website URL format")
            .When(x => !string.IsNullOrEmpty(x.Website));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
