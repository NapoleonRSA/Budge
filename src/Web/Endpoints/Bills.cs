using Budge.Application.Bills.Commands.CreateBill;
using Budge.Application.Bills.Commands.DeleteBill;
using Budge.Application.Bills.Commands.UpdateBill;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class Bills : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateBill);
        groupBuilder.MapPut(UpdateBill, "{id}");
        groupBuilder.MapDelete(DeleteBill, "{id}");
    }

    [EndpointSummary("Add a bill")]
    [EndpointDescription("Adds a bill and assigns it to a person.")]
    public static async Task<Created<int>> CreateBill(ISender sender, CreateBillCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/Bills/{id}", id);
    }

    [EndpointSummary("Update a bill")]
    [EndpointDescription("Updates a manually entered bill. Credit-facility payments are updated with the facility.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateBill(ISender sender, int id, UpdateBillCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove a bill")]
    [EndpointDescription("Removes a manually entered bill.")]
    public static async Task<NoContent> DeleteBill(ISender sender, int id)
    {
        await sender.Send(new DeleteBillCommand(id));

        return TypedResults.NoContent();
    }
}
