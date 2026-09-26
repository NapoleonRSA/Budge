using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;

namespace Budge.Application.Expenses.Commands.CreateExpense;

[Authorize]
public record CreateExpenseCommand : IRequest<int>
{
    public int PersonId { get; init; }

    public int CategoryId { get; init; }

    public decimal Amount { get; init; }

    public DateOnly SpentOn { get; init; }

    public string? Note { get; init; }
}

public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateExpenseCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.PersonId)
            .MustAsync(PersonExists)
                .WithMessage("Choose a person.");

        RuleFor(v => v.CategoryId)
            .MustAsync(CategoryExists)
                .WithMessage("Choose a category.");

        RuleFor(v => v.Amount)
            .GreaterThan(0);

        RuleFor(v => v.SpentOn)
            .Must(date => date != default)
                .WithMessage("Choose the day this was spent.");

        RuleFor(v => v.Note)
            .MaximumLength(300);
    }

    public async Task<bool> PersonExists(int personId, CancellationToken cancellationToken)
    {
        return await _context.People.AnyAsync(p => p.Id == personId, cancellationToken);
    }

    public async Task<bool> CategoryExists(int categoryId, CancellationToken cancellationToken)
    {
        return await _context.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateExpenseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var entity = new Expense
        {
            PersonId = request.PersonId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            SpentOn = request.SpentOn,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _context.Expenses.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
