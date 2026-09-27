namespace Budge.Domain.Entities;

public class CreditFacility : BaseAuditableEntity
{
    public int PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public FacilityKind Kind { get; set; } = FacilityKind.Revolving;

    public FacilityType Type { get; set; } = FacilityType.RevolvingFacility;

    public int? TermMonths { get; set; }

    public decimal Balance { get; set; }

    public decimal AnnualInterestRate { get; set; }

    public decimal MonthlyPayment { get; set; }

    public decimal MonthlyAdminFee { get; set; }

    public int DueDay { get; set; }

    public Bill? PaymentBill { get; set; }
}
