namespace Budge.Domain.Services;

public static class CreditPayoffCalculator
{
    public const int MaxMonths = 600;

    public static string PaymentBillName(string facilityName) => $"{facilityName.Trim()} payment";

    public static decimal MonthlyRate(decimal annualInterestPercent) =>
        (annualInterestPercent / 100m) / 12m;

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static CreditMonth AdvanceMonth(
        decimal balance,
        decimal annualInterestPercent,
        decimal monthlyPayment,
        decimal monthlyAdminFee = 0)
    {
        balance = RoundMoney(balance);
        monthlyPayment = RoundMoney(Math.Max(0, monthlyPayment));
        monthlyAdminFee = RoundMoney(Math.Max(0, monthlyAdminFee));

        if (balance <= 0)
        {
            return new CreditMonth(0, 0, 0, 0, true);
        }

        var interest = RoundMoney(balance * MonthlyRate(Math.Max(0, annualInterestPercent)));
        var due = RoundMoney(balance + interest + monthlyAdminFee);
        var payment = Math.Min(monthlyPayment, due);
        var newBalance = RoundMoney(Math.Max(0, due - payment));
        var coversCharges = payment > interest + monthlyAdminFee || newBalance == 0;

        return new CreditMonth(interest, monthlyAdminFee, payment, newBalance, coversCharges);
    }

    public static CreditPayoffProjection Calculate(
        decimal balance,
        decimal annualInterestPercent,
        decimal monthlyPayment,
        DateOnly asOf,
        decimal monthlyAdminFee = 0,
        decimal extraMonthlyPayment = 0,
        decimal oneOffPayment = 0)
    {
        var remaining = RoundMoney(Math.Max(0, balance));
        var lumpSum = RoundMoney(Math.Clamp(oneOffPayment, 0, remaining));
        remaining = RoundMoney(remaining - lumpSum);
        var monthlyRate = MonthlyRate(Math.Max(0, annualInterestPercent));
        var first = ProjectMonth(
            remaining,
            annualInterestPercent,
            monthlyPayment + Math.Max(0, extraMonthlyPayment),
            monthlyAdminFee);

        if (remaining <= 0)
        {
            return new CreditPayoffProjection(true, 0, asOf, 0, 0, 0, 0, monthlyRate);
        }

        if (!first.CoversInterest)
        {
            return CreditPayoffProjection.Never(monthlyRate, first.Interest);
        }

        decimal totalInterest = 0;
        decimal totalFees = 0;
        decimal totalPaid = lumpSum;
        var months = 0;

        while (remaining > 0 && months < MaxMonths)
        {
            var month = ProjectMonth(
                remaining,
                annualInterestPercent,
                monthlyPayment + Math.Max(0, extraMonthlyPayment),
                monthlyAdminFee);

            if (!month.CoversInterest)
            {
                return CreditPayoffProjection.Never(monthlyRate, first.Interest);
            }

            totalInterest += month.Interest;
            totalFees += month.Fee;
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
            totalFees,
            totalPaid,
            first.Interest,
            monthlyRate);
    }

    public static decimal PaymentForTerm(
        decimal principal,
        decimal annualInterestPercent,
        int termMonths,
        decimal monthlyAdminFee = 0)
    {
        principal = RoundMoney(principal);

        if (termMonths <= 0 || principal <= 0)
        {
            return 0;
        }

        var monthlyRate = (double)MonthlyRate(annualInterestPercent);

        if (monthlyRate == 0)
        {
            return RoundMoney(principal / termMonths + monthlyAdminFee);
        }

        var factor = Math.Pow(1 + monthlyRate, termMonths);
        var payment = (double)principal * monthlyRate * factor / (factor - 1);
        var cents = Math.Ceiling(((decimal)payment + monthlyAdminFee) * 100m);

        return cents / 100m;
    }

    public static CreditMonth ProjectMonth(
        decimal balance,
        decimal annualInterestPercent,
        decimal monthlyPayment,
        decimal monthlyAdminFee = 0)
    {
        balance = RoundMoney(balance);
        monthlyPayment = RoundMoney(Math.Max(0, monthlyPayment));
        monthlyAdminFee = RoundMoney(Math.Max(0, monthlyAdminFee));

        if (balance <= 0)
        {
            return new CreditMonth(0, 0, 0, 0, true);
        }

        var interest = RoundMoney(balance * MonthlyRate(Math.Max(0, annualInterestPercent)));
        var due = RoundMoney(balance + interest + monthlyAdminFee);
        var payment = Math.Min(monthlyPayment, due);
        var newBalance = RoundMoney(Math.Max(0, due - payment));
        var coversCharges = payment > interest + monthlyAdminFee || newBalance == 0;

        return new CreditMonth(interest, monthlyAdminFee, payment, newBalance, coversCharges);
    }

    public static IReadOnlyList<AmortizationEntry> BuildSchedule(
        decimal balance,
        decimal annualInterestPercent,
        decimal monthlyPayment,
        DateOnly asOf,
        int? termMonths = null,
        decimal monthlyAdminFee = 0,
        decimal extraMonthlyPayment = 0,
        decimal oneOffPayment = 0)
    {
        var rows = new List<AmortizationEntry>();
        var opening = RoundMoney(Math.Max(0, balance));
        var remaining = RoundMoney(opening - Math.Clamp(oneOffPayment, 0, opening));

        if (remaining <= 0)
        {
            return rows;
        }

        var limit = MaxMonths;
        var growingLimit = termMonths is > 0 ? Math.Min(termMonths.Value, MaxMonths) : 24;
        var date = asOf;
        var illustrated = 0;

        while (remaining > 0 && illustrated < limit)
        {
            var month = ProjectMonth(
                remaining,
                annualInterestPercent,
                monthlyPayment + Math.Max(0, extraMonthlyPayment),
                monthlyAdminFee);
            var principal = RoundMoney(month.PaymentApplied - month.Interest - month.Fee);
            remaining = month.NewBalance;
            illustrated++;
            date = asOf.AddMonths(illustrated);

            rows.Add(new AmortizationEntry(
                illustrated,
                date,
                month.PaymentApplied,
                month.Interest,
                month.Fee,
                principal,
                remaining,
                month.CoversInterest));

            if (!month.CoversInterest && illustrated >= growingLimit)
            {
                break;
            }
        }

        return rows;
    }
}

public readonly record struct AmortizationEntry(
    int Month,
    DateOnly Date,
    decimal Payment,
    decimal Interest,
    decimal Fee,
    decimal Principal,
    decimal Balance,
    bool CoversInterest);

public readonly record struct CreditMonth(
    decimal Interest,
    decimal Fee,
    decimal PaymentApplied,
    decimal NewBalance,
    bool CoversInterest);

public record CreditPayoffProjection(
    bool WillPayOff,
    int? Months,
    DateOnly? PayoffDate,
    decimal? TotalInterest,
    decimal? TotalFees,
    decimal? TotalPaid,
    decimal FirstMonthInterest,
    decimal MonthlyRate)
{
    public static CreditPayoffProjection Never(decimal monthlyRate, decimal firstMonthInterest) =>
        new(false, null, null, null, null, null, firstMonthInterest, monthlyRate);
}
