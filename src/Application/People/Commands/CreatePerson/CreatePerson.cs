using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Entities;

namespace Budge.Application.People.Commands.CreatePerson;

[Authorize]
public record CreatePersonCommand : IRequest<int>
{
    public string? Name { get; init; }
}

public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    private readonly IApplicationDbContext _context;

    public CreatePersonCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUniqueName)
                .WithMessage("'{PropertyName}' must be unique.")
                .WithErrorCode("Unique");
    }

    public async Task<bool> BeUniqueName(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();

        return !await _context.People
            .AnyAsync(p => p.Name == trimmed, cancellationToken);
    }
}

public class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var entity = new Person
        {
            Name = request.Name!.Trim()
        };

        _context.People.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
