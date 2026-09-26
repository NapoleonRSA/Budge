import { useEffect, useState } from 'react';
import { calculatePayoff, payoffSummary } from '../../lib/payoff';
import {
  defaultSpentOn,
  formatLongDate,
  formatMoney,
  formatMonth,
  shiftMonth,
  spentShare,
} from '../../lib/money';
import {
  applyCreditPayment,
  createBill,
  createCategory,
  createCreditFacility,
  createExpense,
  createPerson,
  deleteBill,
  deleteCategory,
  deleteCreditFacility,
  deleteExpense,
  deletePerson,
  getDashboard,
  readProblem,
  updateCategory,
} from './api';
import { QuickAdd } from './QuickAdd';
import './budget.scss';

export function BudgetPage() {
  const today = new Date();
  const [year, setYear] = useState(today.getFullYear());
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [dashboard, setDashboard] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const load = async (nextYear = year, nextMonth = month) => {
    setLoading(true);
    try {
      const data = await getDashboard(nextYear, nextMonth);
      setDashboard(data);
      setError('');
    } catch (problem) {
      setError(readProblem(problem));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let cancelled = false;

    getDashboard(year, month)
      .then((data) => {
        if (!cancelled) {
          setDashboard(data);
          setError('');
        }
      })
      .catch((problem) => {
        if (!cancelled) setError(readProblem(problem));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [year, month]);

  const run = async (action) => {
    setPending(true);
    setError('');
    try {
      await action();
      await load();
      return true;
    } catch (problem) {
      setError(readProblem(problem));
      return false;
    } finally {
      setPending(false);
    }
  };

  const moveMonth = (delta) => {
    const next = shiftMonth(year, month, delta);
    setYear(next.year);
    setMonth(next.month);
  };

  const people = dashboard?.people ?? [];
  const categories = dashboard?.categories ?? [];
  const bills = dashboard?.bills ?? [];

  return (
    <div className="ledger">
      <header className="ledger-head">
        <div>
          <p className="eyebrow">Household ledger</p>
          <h1>{formatMonth(year, month)}</h1>
          <div className="month-switch">
            <button type="button" onClick={() => moveMonth(-1)}>Previous month</button>
            <button type="button" className="secondary" onClick={() => moveMonth(1)}>Next month</button>
          </div>
        </div>
        <dl className="stats">
          <Stat label="Budget" value={formatMoney(dashboard?.totalBudget ?? 0)} />
          <Stat label="Spent" value={formatMoney(dashboard?.totalSpent ?? 0)} />
          <Stat
            label="Left"
            value={formatMoney(dashboard?.totalRemaining ?? 0)}
            tone={(dashboard?.totalRemaining ?? 0) < 0 ? 'bad' : 'good'}
          />
          <Stat label="Bills to pay" value={formatMoney(dashboard?.billsPayable ?? 0)} />
        </dl>
      </header>

      {error && <p className="banner" role="alert">{error}</p>}
      {loading && !dashboard && <p className="status">Opening the ledger…</p>}

      <section className="panel quick-panel" aria-labelledby="quick-add-title">
        <div className="panel-heading">
          <h2 id="quick-add-title">Quick add</h2>
          <p>Assign the spend to a person and a category. It comes off the budget left for this month.</p>
        </div>
        <QuickAdd
          key={`${year}-${month}`}
          people={people}
          categories={categories}
          defaultDate={defaultSpentOn(year, month)}
          pending={pending}
          onSubmit={(expense) => run(() => createExpense(expense))}
        />
      </section>

      <div className="ledger-grid">
        <section className="panel" aria-labelledby="categories-title">
          <div className="panel-heading">
            <h2 id="categories-title">Budgets</h2>
            <p>What is left in each category after this month’s spending.</p>
          </div>
          <ul className="stack">
            {categories.map((category) => (
              <CategoryRow
                key={category.id}
                category={category}
                pending={pending}
                onSave={(monthlyBudget) => run(() => updateCategory(category.id, {
                  id: category.id,
                  name: category.name,
                  monthlyBudget,
                }))}
                onDelete={() => run(() => deleteCategory(category.id))}
              />
            ))}
            {categories.length === 0 && <li className="empty">No categories yet.</li>}
          </ul>
          <AddCategory pending={pending} onSubmit={(body) => run(() => createCategory(body))} />
        </section>

        <section className="panel" aria-labelledby="people-title">
          <div className="panel-heading">
            <h2 id="people-title">People</h2>
            <p>Spend recorded against each person this month.</p>
          </div>
          <ul className="stack">
            {(dashboard?.spendByPerson ?? []).map((person) => (
              <li key={person.personId} className="row">
                <div>
                  <strong>{person.name}</strong>
                  <span>{formatMoney(person.spent)} spent</span>
                </div>
                <button
                  type="button"
                  className="quiet"
                  disabled={pending}
                  onClick={() => run(() => deletePerson(person.personId))}
                >
                  Remove
                </button>
              </li>
            ))}
            {people.length === 0 && <li className="empty">Add the people who share this budget.</li>}
          </ul>
          <AddPerson pending={pending} onSubmit={(name) => run(() => createPerson(name))} />
        </section>

        <section className="panel bills-panel" aria-labelledby="bills-title">
          <div className="panel-heading">
            <h2 id="bills-title">Bills</h2>
            <p>Each bill is assigned to the person who pays it. Card payments are included.</p>
          </div>
          {people.map((person) => {
            const assigned = bills.filter((bill) => bill.personId === person.id);
            const total = assigned.reduce((sum, bill) => sum + bill.amount, 0);
            return (
              <div key={person.id} className="bill-group">
                <div className="row">
                  <strong>{person.name}</strong>
                  <span>{formatMoney(total)}</span>
                </div>
                <ul className="stack">
                  {assigned.map((bill) => (
                    <li key={bill.id} className="row">
                      <div>
                        <span>{bill.name}</span>
                        <small>Due day {bill.dueDay}{bill.creditFacilityId ? ' · credit payment' : ''}</small>
                      </div>
                      <div className="row-actions">
                        <strong>{formatMoney(bill.amount)}</strong>
                        {!bill.creditFacilityId && (
                          <button type="button" className="quiet" disabled={pending} onClick={() => run(() => deleteBill(bill.id))}>
                            Remove
                          </button>
                        )}
                      </div>
                    </li>
                  ))}
                  {assigned.length === 0 && <li className="empty">Nothing assigned.</li>}
                </ul>
              </div>
            );
          })}
          <AddBill people={people} pending={pending} onSubmit={(body) => run(() => createBill(body))} />
        </section>

        <section className="panel" aria-labelledby="credit-title">
          <div className="panel-heading">
            <h2 id="credit-title">Credit</h2>
            <p>A set payment is added to that person’s bills. Payoff assumes the same payment every month, after interest.</p>
          </div>
          <ul className="stack">
            {(dashboard?.creditFacilities ?? []).map((facility) => (
              <li key={facility.id} className="credit-card">
                <div className="row">
                  <div>
                    <strong>{facility.name}</strong>
                    <span>Paid by {facility.personName} · due day {facility.dueDay}</span>
                  </div>
                  <strong>{formatMoney(facility.balance)}</strong>
                </div>
                <p className={facility.willPayOff ? 'payoff good' : 'payoff bad'}>
                  {payoffSummary({
                    willPayOff: facility.willPayOff,
                    months: facility.monthsToPayoff,
                    payoffDate: facility.payoffDate,
                    totalInterest: facility.totalInterest,
                    firstMonthInterest: facility.firstMonthInterest,
                  })}
                </p>
                <p className="meta">
                  {facility.annualInterestRate}% APR · payment {formatMoney(facility.monthlyPayment)} · interest this month {formatMoney(facility.firstMonthInterest)}
                </p>
                <div className="row-actions">
                  <button
                    type="button"
                    disabled={pending || facility.balance <= 0}
                    onClick={() => run(() => applyCreditPayment(facility.id))}
                  >
                    Apply set payment
                  </button>
                  <button type="button" className="quiet" disabled={pending} onClick={() => run(() => deleteCreditFacility(facility.id))}>
                    Remove
                  </button>
                </div>
              </li>
            ))}
            {(dashboard?.creditFacilities ?? []).length === 0 && <li className="empty">No credit cards or facilities yet.</li>}
          </ul>
          <AddCredit people={people} pending={pending} onSubmit={(body) => run(() => createCreditFacility(body))} />
        </section>
      </div>

      <section className="panel" aria-labelledby="activity-title">
        <div className="panel-heading">
          <h2 id="activity-title">Spending this month</h2>
        </div>
        <ul className="stack">
          {(dashboard?.expenses ?? []).map((expense) => (
            <li key={expense.id} className="row">
              <div>
                <strong>{formatMoney(expense.amount)}</strong>
                <span>{expense.personName} · {expense.categoryName}</span>
                <small>{formatLongDate(expense.spentOn)}{expense.note ? ` · ${expense.note}` : ''}</small>
              </div>
              <button type="button" className="quiet" disabled={pending} onClick={() => run(() => deleteExpense(expense.id))}>
                Remove
              </button>
            </li>
          ))}
          {(dashboard?.expenses ?? []).length === 0 && <li className="empty">No spending in this month.</li>}
        </ul>
      </section>
    </div>
  );
}

function Stat({ label, value, tone }) {
  return (
    <div className={tone ? `stat ${tone}` : 'stat'}>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function CategoryRow({ category, pending, onSave, onDelete }) {
  const [budget, setBudget] = useState(String(category.monthlyBudget));
  const share = spentShare(category.monthlyBudget, category.spent);
  const over = category.remaining < 0;

  return (
    <li className="category">
      <div className="row">
        <strong>{category.name}</strong>
        <span className={over ? 'bad' : 'good'}>{formatMoney(category.remaining)} left</span>
      </div>
      <div className={over ? 'meter over' : 'meter'} aria-hidden="true">
        <span style={{ width: `${share}%` }} />
      </div>
      <p className="meta">{formatMoney(category.spent)} of {formatMoney(category.monthlyBudget)}</p>
      <form
        className="inline-form"
        onSubmit={(event) => {
          event.preventDefault();
          onSave(Number(budget));
        }}
      >
        <label htmlFor={`budget-${category.id}`}>Monthly budget</label>
        <input
          id={`budget-${category.id}`}
          type="number"
          min="0"
          step="0.01"
          value={budget}
          onChange={(event) => setBudget(event.target.value)}
        />
        <button type="submit" className="secondary" disabled={pending}>Save</button>
        <button type="button" className="quiet" disabled={pending} onClick={onDelete}>Remove</button>
      </form>
    </li>
  );
}

function AddPerson({ pending, onSubmit }) {
  const [name, setName] = useState('');

  return (
    <form
      className="add-form"
      onSubmit={async (event) => {
        event.preventDefault();
        const saved = await onSubmit(name.trim());
        if (saved) setName('');
      }}
    >
      <label htmlFor="new-person">Add a person</label>
      <div className="inline-form">
        <input id="new-person" value={name} onChange={(event) => setName(event.target.value)} required maxLength={100} />
        <button type="submit" disabled={pending}>Add person</button>
      </div>
    </form>
  );
}

function AddCategory({ pending, onSubmit }) {
  const [name, setName] = useState('');
  const [monthlyBudget, setMonthlyBudget] = useState('');

  return (
    <form
      className="add-form"
      onSubmit={async (event) => {
        event.preventDefault();
        const saved = await onSubmit({ name: name.trim(), monthlyBudget: Number(monthlyBudget) });
        if (saved) {
          setName('');
          setMonthlyBudget('');
        }
      }}
    >
      <label htmlFor="new-category">Add a category</label>
      <div className="inline-form">
        <input id="new-category" placeholder="Name" value={name} onChange={(event) => setName(event.target.value)} required maxLength={100} />
        <input
          aria-label="Monthly budget for the new category"
          type="number"
          min="0"
          step="0.01"
          placeholder="Budget"
          value={monthlyBudget}
          onChange={(event) => setMonthlyBudget(event.target.value)}
          required
        />
        <button type="submit" disabled={pending}>Add category</button>
      </div>
    </form>
  );
}

function AddBill({ people, pending, onSubmit }) {
  const [form, setForm] = useState({ name: '', amount: '', dueDay: '1', personId: '' });
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));

  return (
    <form
      className="add-form"
      onSubmit={async (event) => {
        event.preventDefault();
        const saved = await onSubmit({
          name: form.name.trim(),
          amount: Number(form.amount),
          dueDay: Number(form.dueDay),
          personId: Number(form.personId),
        });
        if (saved) setForm({ name: '', amount: '', dueDay: '1', personId: form.personId });
      }}
    >
      <p className="form-title">Add a bill</p>
      <div className="inline-form">
        <input aria-label="Bill name" placeholder="Name" value={form.name} onChange={update('name')} required maxLength={140} />
        <input aria-label="Bill amount" type="number" min="0.01" step="0.01" placeholder="Amount" value={form.amount} onChange={update('amount')} required />
        <input aria-label="Due day" type="number" min="1" max="31" value={form.dueDay} onChange={update('dueDay')} required />
        <select aria-label="Person who pays the bill" value={form.personId} onChange={update('personId')} required>
          <option value="">Person</option>
          {people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
        </select>
        <button type="submit" disabled={pending || people.length === 0}>Add bill</button>
      </div>
    </form>
  );
}

function AddCredit({ people, pending, onSubmit }) {
  const [form, setForm] = useState({
    name: '',
    personId: '',
    balance: '',
    annualInterestRate: '',
    monthlyPayment: '',
    dueDay: '1',
  });
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));
  const payment = Number(form.monthlyPayment);
  const preview = payment > 0
    ? calculatePayoff({
      balance: Number(form.balance || 0),
      annualInterestPercent: Number(form.annualInterestRate || 0),
      monthlyPayment: payment,
      asOf: new Date().toISOString().slice(0, 10),
    })
    : null;

  return (
    <form
      className="add-form"
      onSubmit={async (event) => {
        event.preventDefault();
        const saved = await onSubmit({
          name: form.name.trim(),
          personId: Number(form.personId),
          balance: Number(form.balance),
          annualInterestRate: Number(form.annualInterestRate),
          monthlyPayment: payment,
          dueDay: Number(form.dueDay),
        });
        if (saved) {
          setForm({
            name: '',
            personId: form.personId,
            balance: '',
            annualInterestRate: '',
            monthlyPayment: '',
            dueDay: '1',
          });
        }
      }}
    >
      <p className="form-title">Add a credit card or facility</p>
      <div className="inline-form">
        <input aria-label="Facility name" placeholder="Name" value={form.name} onChange={update('name')} required maxLength={120} />
        <select aria-label="Person who pays the facility" value={form.personId} onChange={update('personId')} required>
          <option value="">Person</option>
          {people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
        </select>
        <input aria-label="Balance" type="number" min="0" step="0.01" placeholder="Balance" value={form.balance} onChange={update('balance')} required />
        <input aria-label="Annual interest rate" type="number" min="0" max="100" step="0.01" placeholder="APR %" value={form.annualInterestRate} onChange={update('annualInterestRate')} required />
        <input aria-label="Set monthly payment" type="number" min="0.01" step="0.01" placeholder="Payment" value={form.monthlyPayment} onChange={update('monthlyPayment')} required />
        <input aria-label="Payment due day" type="number" min="1" max="31" value={form.dueDay} onChange={update('dueDay')} required />
        <button type="submit" disabled={pending || people.length === 0}>Add facility</button>
      </div>
      {preview && <p className="preview">{payoffSummary(preview)}</p>}
    </form>
  );
}
