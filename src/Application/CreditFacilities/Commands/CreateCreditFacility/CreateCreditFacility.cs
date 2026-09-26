using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;
using Budge.Domain.Services;

namespace Budge.Application.CreditFacilities.Commands.CreateCreditFacility;

[Authorize]
public record CreateCreditFacilityCommand : IRequest<int>
{
    public int PersonId { get; init; }

    public string? Name { get; init; }

    public decimal Balance { get; init; }

    public decimal AnnualInterestRate { get; init; }

    public decimal MonthlyPayment { get; init; }

    public int DueDay { get; init; }
}

public class CreateCreditFacilityCommandValidator : AbstractValidator<CreateCreditFacilityCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateCreditFacilityCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.PersonId)
            .MustAsync(PersonExists)
                .WithMessage("Choose who pays this facility.");

        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(v => v.Balance)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.AnnualInterestRate)
            .InclusiveBetween(0, 100);

        RuleFor(v => v.MonthlyPayment)
            .GreaterThan(0);

        RuleFor(v => v.DueDay)
            .InclusiveBetween(1, 31);
    }

    public async Task<bool> PersonExists(int personId, CancellationToken cancellationToken)
    {
        return await _context.People.AnyAsync(p => p.Id == personId, cancellationToken);
    }
}

public class CreateCreditFacilityCommandHandler : IRequestHandler<CreateCreditFacilityCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateCreditFacilityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCreditFacilityCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name!.Trim();
        var facility = new CreditFacility
        {
            PersonId = request.PersonId,
            Name = name,
            Balance = request.Balance,
            AnnualInterestRate = request.AnnualInterestRate,
            MonthlyPayment = request.MonthlyPayment,
            DueDay = request.DueDay
        };

        var bill = new Bill
        {
            PersonId = request.PersonId,
            Name = CreditPayoffCalculator.PaymentBillName(name),
            Amount = request.MonthlyPayment,
            DueDay = request.DueDay,
            CreditFacility = facility
        };

        _context.CreditFacilities.Add(facility);
        _context.Bills.Add(bill);

        await _context.SaveChangesAsync(cancellationToken);

        return facility.Id;
    }
}
