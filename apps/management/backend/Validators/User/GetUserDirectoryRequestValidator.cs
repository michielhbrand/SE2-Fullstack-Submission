using FluentValidation;
using ManagementApi.DTOs.User;

namespace ManagementApi.Validators.User;

public class GetUserDirectoryRequestValidator : AbstractValidator<GetUserDirectoryRequest>
{
    private static readonly string[] ValidSortFields = { "Email", "FirstName", "LastName", "Active" };

    public GetUserDirectoryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || ValidSortFields.Contains(sortBy))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("SearchTerm must not exceed 100 characters");
    }
}
