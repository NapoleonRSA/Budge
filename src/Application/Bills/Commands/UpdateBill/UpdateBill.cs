using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.Bills.Commands.UpdateBill;

[Authorize]
public record UpdateBillCommand : IRequest
{
    public int Id { get; init; }

    public int PersonId { get; init; }

    public string? Name { get; init; }

    public decimal Amount { get; init; }

    public int DueDay { get; init; }
}

public class UpdateBillCommandValidator : AbstractValidator<UpdateBillCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateBillCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.PersonId)
            .MustAsync(PersonExists)
                .WithMessage("Choose a person for this bill.");

        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(140);

        RuleFor(v => v.Amount)
            .GreaterThan(0);

        RuleFor(v => v.DueDay)
            .InclusiveBetween(1, 31);
    }

    public async Task<bool> PersonExists(int personId, CancellationToken cancellationToken)
    {
        return await _context.People.AnyAsync(p => p.Id == personId, cancellationToken);
    }
}

public class UpdateBillCommandHandler : IRequestHandler<UpdateBillCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateBillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateBillCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Bills
            .SingleOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        if (entity.CreditFacilityId is not null)
        {
            Budge.Application.Common.Exceptions.ValidationException.ThrowFor(nameof(request.Id), "Change the credit facility to update this payment.");
        }

        entity.PersonId = request.PersonId;
        entity.Name = request.Name!.Trim();
        entity.Amount = request.Amount;
        entity.DueDay = request.DueDay;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
