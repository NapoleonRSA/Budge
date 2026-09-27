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
        result.TotalFees.ShouldBe(0);
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
        result.TotalFees.ShouldBe(0);
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
    public void ShouldAddMonthlyAdminFeeToBalanceBeforePayment()
    {
        var month = CreditPayoffCalculator.AdvanceMonth(100m, 12m, 12m, 2m);

        month.Interest.ShouldBe(1m);
        month.Fee.ShouldBe(2m);
        month.PaymentApplied.ShouldBe(12m);
        month.NewBalance.ShouldBe(91m);
    }

    [Test]
    public void ShouldCompareMonthlyAndOneOffExtrasIncludingFees()
    {
        var result = CreditPayoffCalculator.Calculate(
            100m,
            0m,
            25m,
            AsOf,
            monthlyAdminFee: 5m,
            extraMonthlyPayment: 10m,
            oneOffPayment: 20m);

        result.WillPayOff.ShouldBeTrue();
        result.Months.ShouldBe(3);
        result.PayoffDate.ShouldBe(new DateOnly(2026, 4, 1));
        result.TotalInterest.ShouldBe(0);
        result.TotalFees.ShouldBe(15m);
        result.TotalPaid.ShouldBe(115m);
    }

    [Test]
    public void ShouldShowDebtGrowingWhenMinimumDoesNotCoverInterestAndFee()
    {
        var result = CreditPayoffCalculator.Calculate(100m, 12m, 3m, AsOf, monthlyAdminFee: 2m);
        var schedule = CreditPayoffCalculator.BuildSchedule(100m, 12m, 3m, AsOf, monthlyAdminFee: 2m);

        result.WillPayOff.ShouldBeFalse();
        result.FirstMonthInterest.ShouldBe(1m);
        result.TotalFees.ShouldBeNull();
        schedule.First().Fee.ShouldBe(2m);
        schedule.First().Balance.ShouldBe(100m);
    }

    [Test]
    public void ShouldIncludeMonthlyFeeInTermPaymentEstimate()
    {
        CreditPayoffCalculator.PaymentForTerm(12000m, 0m, 12, 5m).ShouldBe(1005m);
    }

    [Test]
    public void ShouldNameTheBillAfterTheFacility()
    {
        CreditPayoffCalculator.PaymentBillName(" Visa ").ShouldBe("Visa payment");
    }

    [Test]
    public void ShouldPriceATermLoanAndFinishOnThatSchedule()
    {
        var payment = CreditPayoffCalculator.PaymentForTerm(12000m, 0m, 12);

        payment.ShouldBe(1000m);

        var schedule = CreditPayoffCalculator.BuildSchedule(12000m, 0m, payment, AsOf, 12);

        schedule.Count.ShouldBe(12);
        schedule.Last().Balance.ShouldBe(0);
        schedule.Sum(row => row.Principal).ShouldBe(12000m);
    }

    [Test]
    public void ShouldFinishARoundedMortgageInsideItsTerm()
    {
        var payment = CreditPayoffCalculator.PaymentForTerm(485000m, 7.15m, 360);
        var schedule = CreditPayoffCalculator.BuildSchedule(485000m, 7.15m, payment, AsOf, 360);

        schedule.Last().Balance.ShouldBe(0);
        schedule.Count.ShouldBeLessThanOrEqualTo(360);
    }

    [Test]
    public void ShouldShowARevolvingBalanceGrowingWhenThePaymentMissesInterest()
    {
        var schedule = CreditPayoffCalculator.BuildSchedule(1000m, 12m, 5m, AsOf);

        schedule.Count.ShouldBe(24);
        schedule.Last().Balance.ShouldBe(1134.85m);
        schedule.ShouldAllBe(row => !row.CoversInterest);
    }
}
