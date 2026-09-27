using Budge.Application.Budget.Commands.UpdateCurrency;
using Budge.Application.Budget.Queries.GetBudgetDashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class Budget : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetBudgetDashboard);
        groupBuilder.MapPut(UpdateCurrency, "currency");
    }

    [EndpointSummary("Get the household ledger")]
    [EndpointDescription("Returns people, category budgets left for the month, spending by person, bills, and credit payoff plans.")]
    public static async Task<Ok<BudgetDashboardDto>> GetBudgetDashboard(ISender sender, int? year, int? month)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dashboard = await sender.Send(new GetBudgetDashboardQuery(year ?? today.Year, month ?? today.Month));

        return TypedResults.Ok(dashboard);
    }

    [EndpointSummary("Change the ledger currency")]
    [EndpointDescription("Sets the ISO currency used to show amounts. Existing numbers are not converted.")]
    public static async Task<NoContent> UpdateCurrency(ISender sender, UpdateCurrencyCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
