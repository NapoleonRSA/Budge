using Budge.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace Budge.Domain.UnitTests.Services;

public class CreditPayoffCalculatorTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 1);

    [Test]
    public void ShouldTreatAZeroBalanceAsPaidOff()
    {
        var result = CreditPayoffCalculator.Calculate(0, 19.9m, 50m, AsOf);

        result.WillPayOff.ShouldBeTrue();
        result.Months.ShouldBe(0);
        result.PayoffDate.ShouldBe(AsOf);
        result.TotalInterest.ShouldBe(0);
        result.TotalPaid.ShouldBe(0);
    }

    [Test]
    public void ShouldPayOffWithoutInterestInEvenInstalments()
    {
        var result = CreditPayoffCalculator.Calculate(1000m, 0m, 100m, AsOf);

        result.WillPayOff.ShouldBeTrue();
        result.Months.ShouldBe(10);
        result.PayoffDate.ShouldBe(new DateOnly(2026, 11, 1));
        result.FirstMonthInterest.ShouldBe(0);
        result.TotalInterest.ShouldBe(0);
        result.TotalPaid.ShouldBe(1000m);
    }

    [Test]
    public void ShouldChargeInterestAndFinishOnAPartialLastPayment()
    {
        var result = CreditPayoffCalculator.Calculate(100m, 12m, 50m, AsOf);

        result.WillPayOff.ShouldBeTrue();
        result.Months.ShouldBe(3);
        result.FirstMonthInterest.ShouldBe(1m);
        result.TotalInterest.ShouldBe(1.53m);
        result.TotalPaid.ShouldBe(101.53m);
        result.PayoffDate.ShouldBe(new DateOnly(2026, 4, 1));
    }

    [Test]
    public void ShouldRefuseToPayOffWhenThePaymentDoesNotCoverInterest()
    {
        var result = CreditPayoffCalculator.Calculate(1000m, 12m, 10m, AsOf);

        result.WillPayOff.ShouldBeFalse();
        result.Months.ShouldBeNull();
        result.PayoffDate.ShouldBeNull();
        result.FirstMonthInterest.ShouldBe(10m);
        result.TotalInterest.ShouldBeNull();
    }

    [Test]
    public void ShouldReduceTheBalanceByPrincipalAfterInterest()
    {
        var month = CreditPayoffCalculator.AdvanceMonth(100m, 12m, 50m);

        month.CoversInterest.ShouldBeTrue();
        month.Interest.ShouldBe(1m);
        month.PaymentApplied.ShouldBe(50m);
        month.NewBalance.ShouldBe(51m);
    }

    [Test]
    public void ShouldNameTheBillAfterTheFacility()
    {
        CreditPayoffCalculator.PaymentBillName(" Visa ").ShouldBe("Visa payment");
    }
}
