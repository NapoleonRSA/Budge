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
  const target = new Date(Date.UTC(year, month - 1 + months, 1));
  const y = target.getUTCFullYear();
  const monthIndex = target.getUTCMonth();
  const m = String(monthIndex + 1).padStart(2, '0');
  const lastDay = new Date(Date.UTC(y, monthIndex + 1, 0)).getUTCDate();
  const d = String(Math.min(day, lastDay)).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function advanceMonth(balance, annualInterestPercent, monthlyPayment, monthlyAdminFee = 0) {
  const balanceCents = Math.max(0, toCents(balance));
  const paymentCents = Math.max(0, toCents(monthlyPayment));
  const feeCents = Math.max(0, toCents(monthlyAdminFee));

  if (balanceCents === 0) {
    return { interest: 0, fee: 0, paymentApplied: 0, newBalance: 0, coversInterest: true };
  }

  const interest = interestCents(balanceCents, annualInterestPercent);
  const due = balanceCents + interest + feeCents;
  const payment = Math.min(paymentCents, due);
  const next = Math.max(0, due - payment);

  if (payment <= interest + feeCents && next > 0) {
    return {
      interest: fromCents(interest),
      fee: fromCents(feeCents),
      paymentApplied: fromCents(payment),
      newBalance: fromCents(next),
      coversInterest: false,
    };
  }

  return {
    interest: fromCents(interest),
    fee: fromCents(feeCents),
    paymentApplied: fromCents(payment),
    newBalance: fromCents(next),
    coversInterest: true,
  };
}

export function calculatePayoff({
  balance,
  annualInterestPercent,
  monthlyPayment,
  asOf,
  monthlyAdminFee = 0,
  extraMonthlyPayment = 0,
  oneOffPayment = 0,
}) {
  const original = Math.max(0, toCents(balance));
  const lumpSum = Math.min(original, Math.max(0, toCents(oneOffPayment)));
  const start = original - lumpSum;
  const payment = Math.max(0, Number(monthlyPayment) || 0) + Math.max(0, Number(extraMonthlyPayment) || 0);
  const first = advanceMonth(fromCents(start), annualInterestPercent, payment, monthlyAdminFee);

  if (start === 0) {
    return {
      willPayOff: true,
      months: 0,
      payoffDate: asOf,
      totalInterest: 0,
      totalFees: 0,
      totalPaid: fromCents(lumpSum),
      firstMonthInterest: 0,
      firstMonthFee: 0,
    };
  }

  if (!first.coversInterest) {
    return {
      willPayOff: false,
      months: null,
      payoffDate: null,
      totalInterest: null,
      totalFees: null,
      totalPaid: null,
      firstMonthInterest: first.interest,
      firstMonthFee: first.fee,
    };
  }

  let remaining = start;
  let months = 0;
  let totalInterest = 0;
  let totalFees = 0;
  let totalPaid = lumpSum;

  while (remaining > 0 && months < MAX_MONTHS) {
    const month = advanceMonth(fromCents(remaining), annualInterestPercent, payment, monthlyAdminFee);
    if (!month.coversInterest) {
      return {
        willPayOff: false,
        months: null,
        payoffDate: null,
        totalInterest: null,
        totalFees: null,
        totalPaid: null,
        firstMonthInterest: first.interest,
        firstMonthFee: first.fee,
      };
    }

    totalInterest += toCents(month.interest);
    totalFees += toCents(month.fee);
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
      totalFees: null,
      totalPaid: null,
      firstMonthInterest: first.interest,
      firstMonthFee: first.fee,
    };
  }

  return {
    willPayOff: true,
    months,
    payoffDate: addMonths(asOf, months),
    totalInterest: fromCents(totalInterest),
    totalFees: fromCents(totalFees),
    totalPaid: fromCents(totalPaid),
    firstMonthInterest: first.interest,
    firstMonthFee: first.fee,
  };
}

export function buildSchedule({
  balance,
  annualInterestPercent,
  monthlyPayment,
  asOf,
  termMonths,
  monthlyAdminFee = 0,
  extraMonthlyPayment = 0,
  oneOffPayment = 0,
}) {
  const original = Math.max(0, toCents(balance));
  let remaining = original - Math.min(original, Math.max(0, toCents(oneOffPayment)));
  const rows = [];
  const limit = Math.max(1, Number(termMonths) || 24);
  const payment = Math.max(0, Number(monthlyPayment) || 0) + Math.max(0, Number(extraMonthlyPayment) || 0);

  for (let monthNumber = 1; remaining > 0 && monthNumber <= MAX_MONTHS; monthNumber += 1) {
    const month = advanceMonth(fromCents(remaining), annualInterestPercent, payment, monthlyAdminFee);
    const principal = toCents(month.paymentApplied) - toCents(month.interest) - toCents(month.fee);
    remaining = toCents(month.newBalance);
    rows.push({
      month: monthNumber,
      date: addMonths(asOf, monthNumber),
      payment: month.paymentApplied,
      interest: month.interest,
      fee: month.fee,
      principal: fromCents(principal),
      balance: month.newBalance,
      coversInterest: month.coversInterest,
    });

    if (!month.coversInterest && monthNumber >= limit) break;
  }

  return rows;
}

export function paymentForTerm(principal, annualInterestPercent, termMonths, monthlyAdminFee = 0) {
  const balance = Math.max(0, Number(principal) || 0);
  const months = Math.round(Number(termMonths) || 0);
  if (balance <= 0 || months <= 0) return 0;
  const rate = Number(annualInterestPercent) / 100 / 12;
  if (!rate) return Math.round((balance / months + Number(monthlyAdminFee || 0)) * 100) / 100;
  const factor = (1 + rate) ** months;
  const raw = balance * rate * factor / (factor - 1);
  return Math.ceil((raw + Number(monthlyAdminFee || 0)) * 100 - 1e-8) / 100;
}

export function yearlyBuckets(schedule) {
  const years = [];
  for (let index = 0; index < schedule.length; index += 12) {
    const slice = schedule.slice(index, index + 12);
    years.push({
      label: String(years.length + 1),
      interest: slice.reduce((sum, row) => sum + Number(row.interest), 0),
      fees: slice.reduce((sum, row) => sum + Number(row.fee || 0), 0),
      principal: slice.reduce((sum, row) => sum + Number(row.principal), 0),
      balance: Number(slice[slice.length - 1].balance),
    });
  }
  return years;
}

export function payoffSummary(result) {
  if (!result.willPayOff) {
    if (result.firstMonthFee > 0) {
      return `This payment does not cover ${formatMoney(result.firstMonthInterest + result.firstMonthFee)} of monthly interest and fees, so the balance will not come down.`;
    }
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
