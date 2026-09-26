using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;

namespace Budge.Application.Categories.Commands.CreateCategory;

[Authorize]
public record CreateCategoryCommand : IRequest<int>
{
    public string? Name { get; init; }

    public decimal MonthlyBudget { get; init; }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUniqueName)
                .WithMessage("'{PropertyName}' must be unique.")
                .WithErrorCode("Unique");

        RuleFor(v => v.MonthlyBudget)
            .GreaterThanOrEqualTo(0);
    }

    public async Task<bool> BeUniqueName(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();

        return !await _context.Categories
            .AnyAsync(c => c.Name == trimmed, cancellationToken);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Category
        {
            Name = request.Name!.Trim(),
            MonthlyBudget = request.MonthlyBudget
        };

        _context.Categories.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
