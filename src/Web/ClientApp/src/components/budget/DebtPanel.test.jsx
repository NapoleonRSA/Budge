import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DebtPanel } from './DebtPanel';
import { buildSchedule } from '../../lib/payoff';

const facility = {
  id: 2,
  name: 'Car loan',
  type: 3,
  kind: 1,
  personName: 'Alex',
  dueDay: 1,
  termMonths: 24,
  balance: 1000,
  annualInterestRate: 0,
  monthlyPayment: 100,
  monthlyAdminFee: 0,
  willPayOff: true,
  monthsToPayoff: 10,
  payoffDate: '2026-11-01',
  totalInterest: 0,
  totalFees: 0,
  firstMonthInterest: 0,
  schedule: buildSchedule({ balance: 1000, annualInterestPercent: 0, monthlyPayment: 100, asOf: '2026-01-01' }),
};

describe('DebtPanel', () => {
  afterEach(cleanup);

  beforeEach(() => {
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      value: vi.fn(() => ({ matches: true })),
    });
  });

  it('recalculates payoff dates and graph when recurring extra changes', async () => {
    const user = userEvent.setup();

    render(<DebtPanel facility={facility} pending={false} onApply={vi.fn()} onDelete={vi.fn()} onEdit={vi.fn()} />);

    const currentDate = screen.getByText('Current payoff').nextElementSibling.textContent;
    await user.type(screen.getByLabelText('Car loan extra each month'), '100');

    const extraDate = screen.getByText('With extras').nextElementSibling.textContent;
    expect(extraDate).not.toBe(currentDate);
    expect(screen.getByText('5 months')).toBeTruthy();
    expect(screen.getByRole('img', { name: 'Car loan balance comparison' })).toBeTruthy();
    expect(screen.getByText('Scenario payment schedule')).toBeTruthy();
  });
});
