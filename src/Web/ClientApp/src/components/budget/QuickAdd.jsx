import { useState } from 'react';

export function QuickAdd({ people, categories, defaultDate, onSubmit, pending }) {
  const [form, setForm] = useState({
    amount: '',
    personId: '',
    categoryId: '',
    spentOn: defaultDate,
    note: '',
  });
  const ready = people.length > 0 && categories.length > 0;

  const update = (field) => (event) => {
    setForm((current) => ({ ...current, [field]: event.target.value }));
  };

  const submit = async (event) => {
    event.preventDefault();
    if (!ready || pending) return;

    const saved = await onSubmit({
      personId: Number(form.personId),
      categoryId: Number(form.categoryId),
      amount: Number(form.amount),
      spentOn: form.spentOn,
      note: form.note.trim(),
    });

    if (saved) {
      setForm((current) => ({ ...current, amount: '', note: '' }));
    }
  };

  return (
    <form className="quick-add" onSubmit={submit}>
      <div>
        <label htmlFor="spend-amount">Amount</label>
        <input
          id="spend-amount"
          name="amount"
          type="number"
          inputMode="decimal"
          min="0.01"
          step="0.01"
          required
          value={form.amount}
          onChange={update('amount')}
          disabled={!ready || pending}
        />
      </div>
      <div>
        <label htmlFor="spend-person">Person</label>
        <select
          id="spend-person"
          name="personId"
          required
          value={form.personId}
          onChange={update('personId')}
          disabled={!ready || pending}
        >
          <option value="">Who spent it</option>
          {people.map((person) => (
            <option key={person.id} value={person.id}>{person.name}</option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="spend-category">Category</label>
        <select
          id="spend-category"
          name="categoryId"
          required
          value={form.categoryId}
          onChange={update('categoryId')}
          disabled={!ready || pending}
        >
          <option value="">Budget it comes from</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>{category.name}</option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="spend-date">Date</label>
        <input
          id="spend-date"
          name="spentOn"
          type="date"
          required
          value={form.spentOn}
          onChange={update('spentOn')}
          disabled={!ready || pending}
        />
      </div>
      <div>
        <label htmlFor="spend-note">Note</label>
        <input
          id="spend-note"
          name="note"
          type="text"
          maxLength={300}
          placeholder="Optional"
          value={form.note}
          onChange={update('note')}
          disabled={!ready || pending}
        />
      </div>
      <button type="submit" disabled={!ready || pending}>
        {pending ? 'Saving…' : 'Add spend'}
      </button>
      {!ready && (
        <p className="quick-add-hint">Add a person and a category before recording spend.</p>
      )}
    </form>
  );
}
