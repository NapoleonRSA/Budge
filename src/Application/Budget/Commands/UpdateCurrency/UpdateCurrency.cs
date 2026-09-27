using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;

namespace Budge.Application.Budget.Commands.UpdateCurrency;

[Authorize]
public record UpdateCurrencyCommand : IRequest
{
    public string? CurrencyCode { get; init; }
}

public class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(v => v.CurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a three-letter ISO code.");
    }
}

public class UpdateCurrencyCommandHandler : IRequestHandler<UpdateCurrencyCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCurrencyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var settings = await _context.LedgerSettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new LedgerSettings();
            _context.LedgerSettings.Add(settings);
        }

        settings.CurrencyCode = request.CurrencyCode!;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
