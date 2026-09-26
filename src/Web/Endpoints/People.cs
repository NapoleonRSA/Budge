using Budge.Application.People.Commands.CreatePerson;
using Budge.Application.People.Commands.DeletePerson;
using Budge.Application.People.Commands.UpdatePerson;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class People : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreatePerson);
        groupBuilder.MapPut(UpdatePerson, "{id}");
        groupBuilder.MapDelete(DeletePerson, "{id}");
    }

    [EndpointSummary("Add a person")]
    [EndpointDescription("Adds someone who can be assigned bills and spending.")]
    public static async Task<Created<int>> CreatePerson(ISender sender, CreatePersonCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/People/{id}", id);
    }

    [EndpointSummary("Rename a person")]
    [EndpointDescription("Updates the person's name. The ID in the URL must match the ID in the payload.")]
    public static async Task<Results<NoContent, BadRequest>> UpdatePerson(ISender sender, int id, UpdatePersonCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove a person")]
    [EndpointDescription("Removes a person who has no bills, spending, or credit facilities.")]
    public static async Task<NoContent> DeletePerson(ISender sender, int id)
    {
        await sender.Send(new DeletePersonCommand(id));

        return TypedResults.NoContent();
    }
}
