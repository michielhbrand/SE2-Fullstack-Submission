using ManagementApi.DTOs.PaymentPlan;
using ManagementApi.Exceptions.Application;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Database.Data;

namespace ManagementApi.Endpoints.PaymentPlan;

public static class UpdatePaymentPlanEndpoint
{
    public static RouteHandlerBuilder MapUpdatePaymentPlan(this IEndpointRouteBuilder group)
    {
        return group.MapPut("/{id:int}", Handle)
            .WithName("UpdatePaymentPlan")
            .WithSummary("Update a payment plan's monthly cost")
            .WithDescription("Updates the monthly cost (in Rand) of an existing payment plan. Plan names and user limits are system-defined and cannot be changed.")
            .WithOpenApi()
            .Produces<PaymentPlanResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdatePaymentPlanRequest>>();
    }

    private static async Task<Results<Ok<PaymentPlanResponse>, ProblemHttpResult>> Handle(
        int id,
        UpdatePaymentPlanRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("UpdatePaymentPlan");

        var plan = await db.PaymentPlans.FindAsync([id], cancellationToken);
        if (plan == null)
        {
            logger.LogWarning("Payment plan with ID {PlanId} not found", id);
            throw new NotFoundException("PaymentPlan", id);
        }

        if (request.MonthlyCostRand.HasValue)
        {
            if (request.MonthlyCostRand.Value < 0)
                throw new ValidationException("Monthly cost cannot be negative");

            plan.MonthlyCostRand = request.MonthlyCostRand.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Payment plan {PlanId} ({PlanName}) cost updated to R{Cost}", id, plan.Name, plan.MonthlyCostRand);

        return TypedResults.Ok(new PaymentPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            MaxUsers = plan.MaxUsers,
            MonthlyCostRand = plan.MonthlyCostRand
        });
    }
}
