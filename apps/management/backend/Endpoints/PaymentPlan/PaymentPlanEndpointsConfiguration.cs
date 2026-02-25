namespace ManagementApi.Endpoints.PaymentPlan;

public static class PaymentPlanEndpointsConfiguration
{
    public static IEndpointRouteBuilder MapPaymentPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payment-plans")
            .RequireAuthorization("SystemAdminOnly")
            .WithTags("PaymentPlans");

        group.MapGetPaymentPlans();
        group.MapUpdatePaymentPlan();

        return app;
    }
}
