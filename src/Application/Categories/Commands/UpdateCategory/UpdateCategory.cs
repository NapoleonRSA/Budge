using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.Categories.Commands.UpdateCategory;

[Authorize]
public record UpdateCategoryCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public decimal MonthlyBudget { get; init; }
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandValidator(IApplicationDbContext context)
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

    public async Task<bool> BeUniqueName(UpdateCategoryCommand command, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();

        return !await _context.Categories
            .AnyAsync(c => c.Id != command.Id && c.Name == trimmed, cancellationToken);
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name!.Trim();
        entity.MonthlyBudget = request.MonthlyBudget;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
