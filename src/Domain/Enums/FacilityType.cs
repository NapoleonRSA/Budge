namespace Budge.Domain.Enums;

public enum FacilityType
{
    RevolvingFacility = 0,
    OtherInstallment = 1,
    HomeLoan = 2,
    CarLoan = 3,
    CreditCard = 4
}

public static class FacilityTypeExtensions
{
    public static bool IsInstallment(this FacilityType type) => type is
        FacilityType.OtherInstallment or FacilityType.HomeLoan or FacilityType.CarLoan;

    public static FacilityKind ToKind(this FacilityType type) =>
        type.IsInstallment() ? FacilityKind.Installment : FacilityKind.Revolving;

    public static FacilityType FromKind(FacilityKind kind) => kind switch
    {
        FacilityKind.Installment => FacilityType.OtherInstallment,
        _ => FacilityType.RevolvingFacility
    };
}
