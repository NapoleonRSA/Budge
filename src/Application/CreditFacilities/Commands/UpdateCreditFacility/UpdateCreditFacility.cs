using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;
using Budge.Domain.Enums;
using Budge.Domain.Services;

namespace Budge.Application.CreditFacilities.Commands.UpdateCreditFacility;

[Authorize]
public record UpdateCreditFacilityCommand : IRequest
{
    public int Id { get; init; }

    public int PersonId { get; init; }

    public string? Name { get; init; }

    public decimal Balance { get; init; }

    public decimal AnnualInterestRate { get; init; }

    public decimal MonthlyPayment { get; init; }

    public int DueDay { get; init; }

    public FacilityKind Kind { get; init; }

    public FacilityType? Type { get; init; }

    public int? TermMonths { get; init; }

    public decimal MonthlyAdminFee { get; init; }
}

public class UpdateCreditFacilityCommandValidator : AbstractValidator<UpdateCreditFacilityCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCreditFacilityCommandValidator(IApplicationDbContext context)
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

        RuleFor(v => v.MonthlyAdminFee)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.Type)
            .Must(type => type is null || Enum.IsDefined(type.Value));

        When(v => (v.Type?.IsInstallment() ?? (v.Kind == FacilityKind.Installment)), () =>
        {
            RuleFor(v => v.TermMonths)
                .NotNull()
                .InclusiveBetween(1, CreditPayoffCalculator.MaxMonths);
        });
    }

    public async Task<bool> PersonExists(int personId, CancellationToken cancellationToken)
    {
        return await _context.People.AnyAsync(p => p.Id == personId, cancellationToken);
    }
}

public class UpdateCreditFacilityCommandHandler : IRequestHandler<UpdateCreditFacilityCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCreditFacilityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCreditFacilityCommand request, CancellationToken cancellationToken)
    {
        var facility = await _context.CreditFacilities
            .Include(f => f.PaymentBill)
            .SingleOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, facility);

        var name = request.Name!.Trim();
        var type = request.Type ?? FacilityTypeExtensions.FromKind(request.Kind);
        facility.PersonId = request.PersonId;
        facility.Name = name;
        facility.Balance = request.Balance;
        facility.AnnualInterestRate = request.AnnualInterestRate;
        facility.MonthlyPayment = request.MonthlyPayment;
        facility.DueDay = request.DueDay;
        facility.Kind = type.ToKind();
        facility.Type = type;
        facility.TermMonths = type.IsInstallment() ? request.TermMonths : null;
        facility.MonthlyAdminFee = request.MonthlyAdminFee;

        if (facility.PaymentBill is null)
        {
            facility.PaymentBill = new Bill();
            _context.Bills.Add(facility.PaymentBill);
        }

        facility.PaymentBill.PersonId = request.PersonId;
        facility.PaymentBill.Name = CreditPayoffCalculator.PaymentBillName(name);
        facility.PaymentBill.Amount = request.MonthlyPayment;
        facility.PaymentBill.DueDay = request.DueDay;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
