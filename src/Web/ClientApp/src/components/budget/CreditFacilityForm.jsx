import { useState } from 'react';
import { paymentForTerm } from '../../lib/payoff';

const TYPE_LABELS = [
  'Revolving facility',
  'Other installment loan',
  'Home loan',
  'Car loan',
  'Credit card',
];

function isInstallment(type) {
  return [1, 2, 3].includes(Number(type));
}

function initialForm(facility) {
  if (!facility) {
    return {
      name: '', personId: '', type: '0', balance: '', annualInterestRate: '',
      termMonths: '240', monthlyPayment: '', monthlyAdminFee: '0', dueDay: '1', lockPayment: false,
    };
  }

  return {
    name: facility.name,
    personId: String(facility.personId),
    type: String(facility.type ?? (facility.kind === 1 ? 1 : 0)),
    balance: String(facility.balance),
    annualInterestRate: String(facility.annualInterestRate),
    termMonths: String(facility.termMonths ?? 240),
    monthlyPayment: String(facility.monthlyPayment),
    monthlyAdminFee: String(facility.monthlyAdminFee ?? 0),
    dueDay: String(facility.dueDay),
    lockPayment: false,
  };
}

export function CreditFacilityForm({ people, pending, facility, onSubmit, onCancel }) {
  const [form, setForm] = useState(() => initialForm(facility));
  const editing = Boolean(facility);
  const installment = isInstallment(form.type);

  const update = (field) => (event) => {
    const value = event.target.type === 'checkbox' ? event.target.checked : event.target.value;
    setForm((current) => {
      const next = { ...current, [field]: value };
      if (next.lockPayment && isInstallment(next.type)) {
        next.monthlyPayment = paymentForTerm(
          next.balance,
          next.annualInterestRate,
          next.termMonths,
          next.monthlyAdminFee,
        ) || '';
      }
      return next;
    });
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    const type = Number(form.type);
    const saved = await onSubmit({
      name: form.name.trim(),
      personId: Number(form.personId),
      type,
      kind: isInstallment(type) ? 1 : 0,
      termMonths: isInstallment(type) ? Number(form.termMonths) : null,
      balance: Number(form.balance),
      annualInterestRate: Number(form.annualInterestRate),
      monthlyPayment: Number(form.monthlyPayment),
      monthlyAdminFee: Number(form.monthlyAdminFee || 0),
      dueDay: Number(form.dueDay),
    });

    if (saved && !editing) setForm(initialForm(null));
  };

  return (
    <form className="add-form" onSubmit={handleSubmit}>
      <p className="form-title">{editing ? `Edit ${facility.name}` : 'Add a loan or credit facility'}</p>
      <div className="inline-form debt-form">
        <label>
          Facility type
          <select aria-label="Facility type" value={form.type} onChange={update('type')}>
            {TYPE_LABELS.map((label, index) => <option key={label} value={index}>{label}</option>)}
          </select>
        </label>
        <label>
          Name
          <input aria-label="Facility name" placeholder="Name" value={form.name} onChange={update('name')} required maxLength={120} />
        </label>
        <label>
          Person who pays
          <select aria-label="Person who pays the facility" value={form.personId} onChange={update('personId')} required>
            <option value="">Choose person</option>
            {people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
          </select>
        </label>
        <label>
          Current balance
          <input aria-label="Balance" type="number" min="0" step="0.01" placeholder="Balance" value={form.balance} onChange={update('balance')} required />
        </label>
        <label>
          Annual interest rate (%)
          <input aria-label="Annual interest rate" type="number" min="0" max="100" step="0.01" placeholder="Rate %" value={form.annualInterestRate} onChange={update('annualInterestRate')} required />
        </label>
        {installment && (
          <label>
            Months remaining
            <input aria-label="Months remaining" type="number" min="1" max="600" step="1" value={form.termMonths} onChange={update('termMonths')} required />
          </label>
        )}
        <label>
          {installment ? 'Required monthly payment' : 'Minimum monthly payment'}
          <input
            aria-label={installment ? 'Required monthly payment' : 'Minimum monthly payment'}
            type="number"
            min="0.01"
            step="0.01"
            placeholder="Payment"
            value={form.monthlyPayment}
            onChange={update('monthlyPayment')}
            required
            readOnly={installment && form.lockPayment}
          />
        </label>
        <label>
          Monthly admin fee
          <input aria-label="Monthly admin fee" type="number" min="0" step="0.01" value={form.monthlyAdminFee} onChange={update('monthlyAdminFee')} />
        </label>
        <label>
          Payment due day
          <input aria-label="Payment due day" type="number" min="1" max="31" value={form.dueDay} onChange={update('dueDay')} required />
        </label>
        <div className="debt-form-actions">
          <button type="submit" disabled={pending || people.length === 0}>{editing ? 'Save facility' : 'Add facility'}</button>
          {editing && <button type="button" className="secondary" disabled={pending} onClick={onCancel}>Cancel</button>}
        </div>
      </div>
      {installment && (
        <label className="lock-payment">
          <input type="checkbox" checked={form.lockPayment} onChange={update('lockPayment')} />
          Estimate payment from balance, rate, remaining term, and monthly fee
        </label>
      )}
    </form>
  );
}
