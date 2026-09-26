using Budge.Application.CreditFacilities.Commands.ApplyCreditPayment;
using Budge.Application.CreditFacilities.Commands.CreateCreditFacility;
using Budge.Application.CreditFacilities.Commands.DeleteCreditFacility;
using Budge.Application.CreditFacilities.Commands.UpdateCreditFacility;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Budge.Web.Endpoints;

public class CreditFacilities : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(CreateCreditFacility);
        groupBuilder.MapPut(UpdateCreditFacility, "{id}");
        groupBuilder.MapPost(ApplyCreditPayment, "{id}/payments");
        groupBuilder.MapDelete(DeleteCreditFacility, "{id}");
    }

    [EndpointSummary("Add a credit facility")]
    [EndpointDescription("Adds a credit card or other facility. Its set payment becomes a bill assigned to the chosen person.")]
    public static async Task<Created<int>> CreateCreditFacility(ISender sender, CreateCreditFacilityCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/CreditFacilities/{id}", id);
    }

    [EndpointSummary("Update a credit facility")]
    [EndpointDescription("Updates the balance, interest rate, set payment, and the person who pays. The linked bill stays in sync.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateCreditFacility(ISender sender, int id, UpdateCreditFacilityCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Apply the set payment")]
    [EndpointDescription("Posts one set payment: interest is charged and the rest reduces the balance.")]
    public static async Task<NoContent> ApplyCreditPayment(ISender sender, int id)
    {
        await sender.Send(new ApplyCreditPaymentCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove a credit facility")]
    [EndpointDescription("Removes the facility and the bill created for its set payment.")]
    public static async Task<NoContent> DeleteCreditFacility(ISender sender, int id)
    {
        await sender.Send(new DeleteCreditFacilityCommand(id));

        return TypedResults.NoContent();
    }
}
