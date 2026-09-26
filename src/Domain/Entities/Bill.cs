namespace Budge.Domain.Entities;

public class Bill : BaseAuditableEntity
{
    public int PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int DueDay { get; set; }

    public int? CreditFacilityId { get; set; }

    public CreditFacility? CreditFacility { get; set; }
}
