using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.Bills.Commands.DeleteBill;

[Authorize]
public record DeleteBillCommand(int Id) : IRequest;

public class DeleteBillCommandHandler : IRequestHandler<DeleteBillCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteBillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteBillCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Bills
            .SingleOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        if (entity.CreditFacilityId is not null)
        {
            Budge.Application.Common.Exceptions.ValidationException.ThrowFor(nameof(request.Id), "Delete the credit facility to remove this payment.");
        }

        _context.Bills.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
