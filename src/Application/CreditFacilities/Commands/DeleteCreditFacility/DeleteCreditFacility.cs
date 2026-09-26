using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.CreditFacilities.Commands.DeleteCreditFacility;

[Authorize]
public record DeleteCreditFacilityCommand(int Id) : IRequest;

public class DeleteCreditFacilityCommandHandler : IRequestHandler<DeleteCreditFacilityCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCreditFacilityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCreditFacilityCommand request, CancellationToken cancellationToken)
    {
        var facility = await _context.CreditFacilities
            .SingleOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, facility);

        _context.CreditFacilities.Remove(facility);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
