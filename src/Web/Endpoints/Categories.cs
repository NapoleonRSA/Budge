using Budge.Application.Categories.Commands.CreateCategory;
using Budge.Application.Categories.Commands.DeleteCategory;
using Budge.Application.Categories.Commands.UpdateCategory;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class Categories : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateCategory);
        groupBuilder.MapPut(UpdateCategory, "{id}");
        groupBuilder.MapDelete(DeleteCategory, "{id}");
    }

    [EndpointSummary("Add a category")]
    [EndpointDescription("Adds a spending category and its monthly budget.")]
    public static async Task<Created<int>> CreateCategory(ISender sender, CreateCategoryCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/Categories/{id}", id);
    }

    [EndpointSummary("Update a category")]
    [EndpointDescription("Updates the category name and monthly budget.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateCategory(ISender sender, int id, UpdateCategoryCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove a category")]
    [EndpointDescription("Removes a category that has no spending.")]
    public static async Task<NoContent> DeleteCategory(ISender sender, int id)
    {
        await sender.Send(new DeleteCategoryCommand(id));

        return TypedResults.NoContent();
    }
}
