namespace Budge.Domain.Entities;

public class LedgerSettings : BaseEntity
{
    public string CurrencyCode { get; set; } = "USD";
}
