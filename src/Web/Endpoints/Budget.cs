using Budge.Application.Budget.Queries.GetBudgetDashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class Budget : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetBudgetDashboard);
    }

    [EndpointSummary("Get the household ledger")]
    [EndpointDescription("Returns people, category budgets left for the month, spending by person, bills, and credit payoff plans.")]
    public static async Task<Ok<BudgetDashboardDto>> GetBudgetDashboard(ISender sender, int? year, int? month)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dashboard = await sender.Send(new GetBudgetDashboardQuery(year ?? today.Year, month ?? today.Month));

        return TypedResults.Ok(dashboard);
    }
}
