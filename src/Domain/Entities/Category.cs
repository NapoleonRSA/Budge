namespace Budge.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal MonthlyBudget { get; set; }

    public IList<Expense> Expenses { get; private set; } = new List<Expense>();
}
