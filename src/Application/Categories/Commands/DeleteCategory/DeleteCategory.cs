using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.Categories.Commands.DeleteCategory;

[Authorize]
public record DeleteCategoryCommand(int Id) : IRequest;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        if (await _context.Expenses.AnyAsync(e => e.CategoryId == request.Id, cancellationToken))
        {
            Budge.Application.Common.Exceptions.ValidationException.ThrowFor(nameof(request.Id), "Remove spending in this category first.");
        }

        _context.Categories.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
