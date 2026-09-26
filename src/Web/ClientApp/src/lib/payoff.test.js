import { describe, expect, it } from 'vitest';
import { calculatePayoff, payoffSummary } from './payoff';

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
});
