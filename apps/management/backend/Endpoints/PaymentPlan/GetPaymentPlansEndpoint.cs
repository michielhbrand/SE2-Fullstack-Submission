using ManagementApi.DTOs.PaymentPlan;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;

namespace ManagementApi.Endpoints.PaymentPlan;

public static class GetPaymentPlansEndpoint
{
    public static RouteHandlerBuilder MapGetPaymentPlans(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetPaymentPlans")
            .WithSummary("Get all payment plans")
            .WithDescription("Retrieves all available payment plans with their user limits and monthly costs")
            .WithOpenApi()
            .Produces<List<PaymentPlanResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Ok<List<PaymentPlanResponse>>> Handle(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var plans = await db.PaymentPlans
            .OrderBy(p => p.Id)
            .Select(p => new PaymentPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                MaxUsers = p.MaxUsers,
                MonthlyCostRand = p.MonthlyCostRand
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(plans);
    }
}
