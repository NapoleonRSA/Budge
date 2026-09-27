import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CreditFacilityForm } from './CreditFacilityForm';

const people = [{ id: 4, name: 'Alex' }];

afterEach(cleanup);

describe('CreditFacilityForm', () => {
  it('submits typed loan terms, minimum payment, and admin fee', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(true);

    render(<CreditFacilityForm people={people} pending={false} onSubmit={onSubmit} />);

    await user.selectOptions(screen.getByLabelText('Facility type'), '2');
    await user.type(screen.getByLabelText('Facility name'), 'Home loan');
    await user.selectOptions(screen.getByLabelText('Person who pays the facility'), '4');
    await user.type(screen.getByLabelText('Balance'), '240000');
    await user.type(screen.getByLabelText('Annual interest rate'), '7.5');
    await user.clear(screen.getByLabelText('Months remaining'));
    await user.type(screen.getByLabelText('Months remaining'), '240');
    await user.type(screen.getByLabelText('Required monthly payment'), '2000');
    await user.clear(screen.getByLabelText('Monthly admin fee'));
    await user.type(screen.getByLabelText('Monthly admin fee'), '25');
    await user.click(screen.getByRole('button', { name: 'Add facility' }));

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Home loan',
      personId: 4,
      type: 2,
      kind: 1,
      termMonths: 240,
      balance: 240000,
      annualInterestRate: 7.5,
      monthlyPayment: 2000,
      monthlyAdminFee: 25,
      dueDay: 1,
    });
  });

  it('prefills saved facility details for editing', () => {
    const facility = {
      id: 8,
      name: 'Visa',
      personId: 4,
      type: 4,
      kind: 0,
      balance: 950,
      annualInterestRate: 19.9,
      termMonths: null,
      monthlyPayment: 50,
      monthlyAdminFee: 3,
      dueDay: 12,
    };

    render(<CreditFacilityForm people={people} pending={false} facility={facility} onSubmit={vi.fn()} onCancel={vi.fn()} />);

    expect(screen.getByLabelText('Facility type').value).toBe('4');
    expect(screen.getByLabelText('Facility name').value).toBe('Visa');
    expect(screen.getByLabelText('Minimum monthly payment').value).toBe('50');
    expect(screen.getByLabelText('Monthly admin fee').value).toBe('3');
    expect(screen.getByRole('button', { name: 'Save facility' })).toBeTruthy();
  });
});
