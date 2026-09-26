import { describe, expect, it } from 'vitest';
import { remainingBudget, spentShare } from './money';

describe('category budget', () => {
  it('subtracts a spend from the budget that is left', () => {
    expect(remainingBudget(100, 25.5)).toBe(74.5);
  });

  it('shows an overspend as a negative remainder', () => {
    expect(remainingBudget(40, 55)).toBe(-15);
    expect(spentShare(40, 55)).toBe(100);
  });

  it('fills the meter from how much of the budget is used', () => {
    expect(spentShare(200, 50)).toBe(25);
  });
});
