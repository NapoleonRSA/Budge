namespace Budge.Domain.Services;

public static class CreditPayoffCalculator
{
    public const int MaxMonths = 600;

    public static string PaymentBillName(string facilityName) => $"{facilityName.Trim()} payment";

    public static decimal MonthlyRate(decimal annualInterestPercent) =>
        (annualInterestPercent / 100m) / 12m;

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static CreditMonth AdvanceMonth(decimal balance, decimal annualInterestPercent, decimal monthlyPayment)
    {
        balance = RoundMoney(balance);
        monthlyPayment = RoundMoney(monthlyPayment);

        if (balance <= 0)
        {
            return new CreditMonth(0, 0, 0, true);
        }

        if (annualInterestPercent < 0)
        {
            annualInterestPercent = 0;
        }

        var interest = RoundMoney(balance * MonthlyRate(annualInterestPercent));

        if (monthlyPayment <= interest)
        {
            return new CreditMonth(interest, 0, balance, false);
        }

        var payment = Math.Min(monthlyPayment, balance + interest);
        var newBalance = RoundMoney((balance + interest) - payment);

        if (newBalance < 0)
        {
            newBalance = 0;
        }

        return new CreditMonth(interest, payment, newBalance, true);
    }

    public static CreditPayoffProjection Calculate(
        decimal balance,
        decimal annualInterestPercent,
        decimal monthlyPayment,
        DateOnly asOf)
    {
        balance = RoundMoney(balance);
        var monthlyRate = MonthlyRate(annualInterestPercent < 0 ? 0 : annualInterestPercent);
        var first = AdvanceMonth(balance, annualInterestPercent, monthlyPayment);

        if (balance <= 0)
        {
            return new CreditPayoffProjection(true, 0, asOf, 0, 0, 0, monthlyRate);
        }

        if (!first.CoversInterest)
        {
            return CreditPayoffProjection.Never(monthlyRate, first.Interest);
        }

        decimal totalInterest = 0;
        decimal totalPaid = 0;
        var remaining = balance;
        var months = 0;

        while (remaining > 0 && months < MaxMonths)
        {
            var month = AdvanceMonth(remaining, annualInterestPercent, monthlyPayment);

            if (!month.CoversInterest)
            {
                return CreditPayoffProjection.Never(monthlyRate, first.Interest);
            }

            totalInterest += month.Interest;
            totalPaid += month.PaymentApplied;
            remaining = month.NewBalance;
            months++;
        }

        if (remaining > 0)
        {
            return CreditPayoffProjection.Never(monthlyRate, first.Interest);
        }

        return new CreditPayoffProjection(
            true,
            months,
            asOf.AddMonths(months),
            totalInterest,
            totalPaid,
            first.Interest,
            monthlyRate);
    }
}

public readonly record struct CreditMonth(
    decimal Interest,
    decimal PaymentApplied,
    decimal NewBalance,
    bool CoversInterest);

public record CreditPayoffProjection(
    bool WillPayOff,
    int? Months,
    DateOnly? PayoffDate,
    decimal? TotalInterest,
    decimal? TotalPaid,
    decimal FirstMonthInterest,
    decimal MonthlyRate)
{
    public static CreditPayoffProjection Never(decimal monthlyRate, decimal firstMonthInterest) =>
        new(false, null, null, null, null, firstMonthInterest, monthlyRate);
}
