namespace ManagementApi.DTOs.PaymentPlan;

public record PaymentPlanResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }

    /// <summary>-1 means unlimited.</summary>
    public int MaxUsers { get; init; }

    public decimal MonthlyCostRand { get; init; }
}

public record UpdatePaymentPlanRequest
{
    public decimal? MonthlyCostRand { get; init; }
}
