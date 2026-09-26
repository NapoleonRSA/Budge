using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;

namespace Budge.Application.People.Commands.UpdatePerson;

[Authorize]
public record UpdatePersonCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }
}

public class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdatePersonCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUniqueName)
                .WithMessage("'{PropertyName}' must be unique.")
                .WithErrorCode("Unique");
    }

    public async Task<bool> BeUniqueName(UpdatePersonCommand command, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();

        return !await _context.People
            .AnyAsync(p => p.Id != command.Id && p.Name == trimmed, cancellationToken);
    }
}

public class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdatePersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.People
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name!.Trim();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
