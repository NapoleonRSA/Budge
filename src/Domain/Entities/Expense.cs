namespace Budge.Domain.Entities;

public class Expense : BaseAuditableEntity
{
    public int PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly SpentOn { get; set; }

    public string? Note { get; set; }
}
