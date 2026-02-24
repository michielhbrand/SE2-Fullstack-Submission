using InvoiceTrackerApi.DTOs.Template.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Template domain models and DTOs
/// </summary>
public static class TemplateMappingExtensions
{
    public static TemplateResponse ToDto(this Template template)
    {
        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Version = template.Version,
            StorageKey = template.StorageKey,
            Created = template.Created,
            CreatedBy = template.CreatedBy
        };
    }
}
