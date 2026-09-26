using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.People.Commands.DeletePerson;

[Authorize]
public record DeletePersonCommand(int Id) : IRequest;

public class DeletePersonCommandHandler : IRequestHandler<DeletePersonCommand>
{
    private readonly IApplicationDbContext _context;

    public DeletePersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.People
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        var hasBills = await _context.Bills.AnyAsync(b => b.PersonId == request.Id, cancellationToken);
        var hasExpenses = await _context.Expenses.AnyAsync(e => e.PersonId == request.Id, cancellationToken);
        var hasCredit = await _context.CreditFacilities.AnyAsync(f => f.PersonId == request.Id, cancellationToken);

        if (hasBills || hasExpenses || hasCredit)
        {
            Budge.Application.Common.Exceptions.ValidationException.ThrowFor(nameof(request.Id), "Remove this person's bills, spending, and credit facilities first.");
        }

        _context.People.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
