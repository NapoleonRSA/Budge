using Budge.Domain.Entities;

namespace Budge.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<Person> People { get; }

    DbSet<Category> Categories { get; }

    DbSet<Expense> Expenses { get; }

    DbSet<Bill> Bills { get; }

    DbSet<CreditFacility> CreditFacilities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
