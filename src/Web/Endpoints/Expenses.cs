using Budge.Application.Expenses.Commands.CreateExpense;
using Budge.Application.Expenses.Commands.DeleteExpense;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class Expenses : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateExpense);
        groupBuilder.MapDelete(DeleteExpense, "{id}");
    }

    [EndpointSummary("Quick add a spend")]
    [EndpointDescription("Records spending against a person and a category. It reduces that category's remaining budget for the month.")]
    public static async Task<Created<int>> CreateExpense(ISender sender, CreateExpenseCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/Expenses/{id}", id);
    }

    [EndpointSummary("Remove a spend")]
    [EndpointDescription("Removes a spend and restores the category budget for that month.")]
    public static async Task<NoContent> DeleteExpense(ISender sender, int id)
    {
        await sender.Send(new DeleteExpenseCommand(id));

        return TypedResults.NoContent();
    }
}
