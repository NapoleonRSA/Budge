using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Services;

namespace Budge.Application.CreditFacilities.Commands.ApplyCreditPayment;

[Authorize]
public record ApplyCreditPaymentCommand(int Id) : IRequest;

public class ApplyCreditPaymentCommandHandler : IRequestHandler<ApplyCreditPaymentCommand>
{
    private readonly IApplicationDbContext _context;

    public ApplyCreditPaymentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ApplyCreditPaymentCommand request, CancellationToken cancellationToken)
    {
        var facility = await _context.CreditFacilities
            .SingleOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, facility);

        var month = CreditPayoffCalculator.AdvanceMonth(
            facility.Balance,
            facility.AnnualInterestRate,
            facility.MonthlyPayment);

        if (!month.CoversInterest)
        {
            Budge.Application.Common.Exceptions.ValidationException.ThrowFor(
                nameof(facility.MonthlyPayment),
                "The set payment does not cover this month's interest, so the balance cannot be paid down.");
        }

        facility.Balance = month.NewBalance;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
