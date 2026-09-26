import { formatLongDate, formatMoney } from './money';

const MAX_MONTHS = 600;

function toCents(value) {
  const amount = Number(value);
  if (!Number.isFinite(amount)) return 0;
  const sign = amount < 0 ? -1 : 1;
  return sign * Math.round(Math.abs(amount) * 100 + 1e-8);
}

function fromCents(cents) {
  return cents / 100;
}

function interestCents(balanceCents, annualPercent) {
  const rate = Number(annualPercent);
  if (balanceCents <= 0 || !Number.isFinite(rate) || rate <= 0) return 0;
  const rateMillis = Math.round(rate * 100 + 1e-8);
  const numerator = balanceCents * rateMillis;
  const denominator = 120000;
  return Math.floor((numerator + Math.floor(denominator / 2)) / denominator);
}

function addMonths(isoDate, months) {
  const [year, month, day] = isoDate.split('-').map(Number);
  const date = new Date(Date.UTC(year, month - 1 + months, day));
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, '0');
  const d = String(date.getUTCDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function advanceMonth(balance, annualInterestPercent, monthlyPayment) {
  const balanceCents = Math.max(0, toCents(balance));
  const paymentCents = Math.max(0, toCents(monthlyPayment));

  if (balanceCents === 0) {
    return { interest: 0, paymentApplied: 0, newBalance: 0, coversInterest: true };
  }

  const interest = interestCents(balanceCents, annualInterestPercent);
  if (paymentCents <= interest) {
    return {
      interest: fromCents(interest),
      paymentApplied: 0,
      newBalance: fromCents(balanceCents),
      coversInterest: false,
    };
  }

  const payment = Math.min(paymentCents, balanceCents + interest);
  const next = Math.max(0, balanceCents + interest - payment);
  return {
    interest: fromCents(interest),
    paymentApplied: fromCents(payment),
    newBalance: fromCents(next),
    coversInterest: true,
  };
}

export function calculatePayoff({ balance, annualInterestPercent, monthlyPayment, asOf }) {
  const start = Math.max(0, toCents(balance));
  const first = advanceMonth(fromCents(start), annualInterestPercent, monthlyPayment);

  if (start === 0) {
    return {
      willPayOff: true,
      months: 0,
      payoffDate: asOf,
      totalInterest: 0,
      totalPaid: 0,
      firstMonthInterest: 0,
    };
  }

  if (!first.coversInterest) {
    return {
      willPayOff: false,
      months: null,
      payoffDate: null,
      totalInterest: null,
      totalPaid: null,
      firstMonthInterest: first.interest,
    };
  }

  let remaining = start;
  let months = 0;
  let totalInterest = 0;
  let totalPaid = 0;

  while (remaining > 0 && months < MAX_MONTHS) {
    const month = advanceMonth(fromCents(remaining), annualInterestPercent, monthlyPayment);
    if (!month.coversInterest) {
      return {
        willPayOff: false,
        months: null,
        payoffDate: null,
        totalInterest: null,
        totalPaid: null,
        firstMonthInterest: first.interest,
      };
    }

    totalInterest += toCents(month.interest);
    totalPaid += toCents(month.paymentApplied);
    remaining = toCents(month.newBalance);
    months += 1;
  }

  if (remaining > 0) {
    return {
      willPayOff: false,
      months: null,
      payoffDate: null,
      totalInterest: null,
      totalPaid: null,
      firstMonthInterest: first.interest,
    };
  }

  return {
    willPayOff: true,
    months,
    payoffDate: addMonths(asOf, months),
    totalInterest: fromCents(totalInterest),
    totalPaid: fromCents(totalPaid),
    firstMonthInterest: first.interest,
  };
}

export function payoffSummary(result) {
  if (!result.willPayOff) {
    return `This payment does not cover ${formatMoney(result.firstMonthInterest)} of interest, so the balance will not come down.`;
  }

  if (!result.months) return 'Paid off.';

  const years = Math.floor(result.months / 12);
  const months = result.months % 12;
  const parts = [];
  if (years) parts.push(`${years} ${years === 1 ? 'year' : 'years'}`);
  if (months) parts.push(`${months} ${months === 1 ? 'month' : 'months'}`);
  const when = result.payoffDate ? ` on ${formatLongDate(result.payoffDate)}` : '';
  return `Paid off in ${parts.join(' ')}${when}. Interest over that time is ${formatMoney(result.totalInterest)}.`;
}
