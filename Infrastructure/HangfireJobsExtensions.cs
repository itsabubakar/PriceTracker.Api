using Hangfire;

public static class HangfireJobsExtensions
{
    public static void RegisterHangfireJobs(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<PriceUpdatedService>(
            "update-all-products",
            x => x.UpdateAllPrices(),
            "*/30 * * * *"
        );

        recurringJobs.AddOrUpdate<AlertJobService>("process-product-alerts", x => x.Run(), "*/30 * * * *");
    }
}
