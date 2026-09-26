import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { QuickAdd } from './QuickAdd';

const people = [{ id: 1, name: 'Alex' }];
const categories = [{ id: 2, name: 'Groceries', monthlyBudget: 100, spent: 0, remaining: 100 }];

describe('QuickAdd', () => {
  it('assigns the spend to a person and a category', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();

    render(
      <QuickAdd
        people={people}
        categories={categories}
        defaultDate="2026-09-02"
        onSubmit={onSubmit}
        pending={false}
      />,
    );

    await user.type(screen.getByLabelText('Amount'), '42.50');
    await user.selectOptions(screen.getByLabelText('Person'), 'Alex');
    await user.selectOptions(screen.getByLabelText('Category'), 'Groceries');
    await user.type(screen.getByLabelText('Note'), 'Market');
    await user.click(screen.getByRole('button', { name: 'Add spend' }));

    expect(onSubmit).toHaveBeenCalledWith({
      personId: 1,
      categoryId: 2,
      amount: 42.5,
      spentOn: '2026-09-02',
      note: 'Market',
    });
  });
});
