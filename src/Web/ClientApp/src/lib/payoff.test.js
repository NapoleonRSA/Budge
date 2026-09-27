import { describe, expect, it } from 'vitest';
import { buildSchedule, calculatePayoff, paymentForTerm, payoffSummary, yearlyBuckets } from './payoff';

const asOf = '2026-01-01';

describe('credit payoff', () => {
  it('treats a zero balance as paid off', () => {
    const result = calculatePayoff({
      balance: 0,
      annualInterestPercent: 19.9,
      monthlyPayment: 50,
      asOf,
    });

    expect(result.willPayOff).toBe(true);
    expect(result.months).toBe(0);
    expect(result.totalInterest).toBe(0);
  });

  it('pays off an interest-free balance in even instalments', () => {
    const result = calculatePayoff({
      balance: 1000,
      annualInterestPercent: 0,
      monthlyPayment: 100,
      asOf,
    });

    expect(result).toMatchObject({
      willPayOff: true,
      months: 10,
      payoffDate: '2026-11-01',
      firstMonthInterest: 0,
      totalInterest: 0,
      totalPaid: 1000,
    });
  });

  it('clamps payoff dates to the last day of shorter months', () => {
    const result = calculatePayoff({
      balance: 100,
      annualInterestPercent: 0,
      monthlyPayment: 100,
      asOf: '2026-01-31',
    });

    expect(result.payoffDate).toBe('2026-02-28');
  });

  it('charges interest and finishes on a partial last payment', () => {
    const result = calculatePayoff({
      balance: 100,
      annualInterestPercent: 12,
      monthlyPayment: 50,
      asOf,
    });

    expect(result).toMatchObject({
      willPayOff: true,
      months: 3,
      payoffDate: '2026-04-01',
      firstMonthInterest: 1,
      totalInterest: 1.53,
      totalPaid: 101.53,
    });
  });

  it('explains when the set payment cannot cover interest', () => {
    const result = calculatePayoff({
      balance: 1000,
      annualInterestPercent: 12,
      monthlyPayment: 10,
      asOf,
    });

    expect(result.willPayOff).toBe(false);
    expect(result.months).toBeNull();
    expect(result.firstMonthInterest).toBe(10);
    expect(payoffSummary(result)).toBe(
      'This payment does not cover $10.00 of interest, so the balance will not come down.',
    );
  });

  it('prices a term loan and groups a schedule by year', () => {
    expect(paymentForTerm(12000, 0, 12)).toBe(1000);
    expect(paymentForTerm(12000, 0, 12, 5)).toBe(1005);

    const years = yearlyBuckets([
      { interest: 10, principal: 90, balance: 910 },
      { interest: 9, principal: 91, balance: 819 },
    ]);

    expect(years).toEqual([
      { label: '1', interest: 19, fees: 0, principal: 181, balance: 819 },
    ]);
  });

  it('compares monthly extras and a lump sum while including monthly fees', () => {
    const result = calculatePayoff({
      balance: 100,
      annualInterestPercent: 0,
      monthlyPayment: 25,
      monthlyAdminFee: 5,
      extraMonthlyPayment: 10,
      oneOffPayment: 20,
      asOf,
    });
    const schedule = buildSchedule({
      balance: 100,
      annualInterestPercent: 0,
      monthlyPayment: 25,
      monthlyAdminFee: 5,
      extraMonthlyPayment: 10,
      oneOffPayment: 20,
      asOf,
    });

    expect(result).toMatchObject({
      willPayOff: true,
      months: 3,
      payoffDate: '2026-04-01',
      totalInterest: 0,
      totalFees: 15,
      totalPaid: 115,
    });
    expect(schedule[0]).toMatchObject({ fee: 5, principal: 30, balance: 50 });
    expect(schedule.at(-1).balance).toBe(0);
  });

  it('reports no payoff when fixed minimum cannot cover interest and monthly fee', () => {
    const result = calculatePayoff({
      balance: 100,
      annualInterestPercent: 12,
      monthlyPayment: 3,
      monthlyAdminFee: 2,
      asOf,
    });

    expect(result.willPayOff).toBe(false);
    expect(result.firstMonthInterest).toBe(1);
    expect(result.firstMonthFee).toBe(2);
    expect(payoffSummary(result)).toContain('$3.00 of monthly interest and fees');
  });
});
