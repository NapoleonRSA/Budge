using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;

namespace Budge.Application.Bills.Commands.CreateBill;

[Authorize]
public record CreateBillCommand : IRequest<int>
{
    public int PersonId { get; init; }

    public string? Name { get; init; }

    public decimal Amount { get; init; }

    public int DueDay { get; init; }
}

public class CreateBillCommandValidator : AbstractValidator<CreateBillCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateBillCommandValidator(IApplicationDbContext context)
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

public class CreateBillCommandHandler : IRequestHandler<CreateBillCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateBillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        var entity = new Bill
        {
            PersonId = request.PersonId,
            Name = request.Name!.Trim(),
            Amount = request.Amount,
            DueDay = request.DueDay
        };

        _context.Bills.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
