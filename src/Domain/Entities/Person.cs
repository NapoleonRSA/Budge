namespace Budge.Domain.Entities;

public class Person : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public IList<Bill> Bills { get; private set; } = new List<Bill>();

    public IList<Expense> Expenses { get; private set; } = new List<Expense>();

    public IList<CreditFacility> CreditFacilities { get; private set; } = new List<CreditFacility>();
}
