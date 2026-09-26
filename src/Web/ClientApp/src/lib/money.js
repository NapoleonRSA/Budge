export function roundMoney(value) {
  const amount = Number(value);
  if (!Number.isFinite(amount)) return 0;
  const sign = amount < 0 ? -1 : 1;
  const cents = Math.round(Math.abs(amount) * 100 + 1e-8);
  return (sign * cents) / 100;
}

export function formatMoney(amount) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(roundMoney(amount));
}

export function remainingBudget(monthlyBudget, spent) {
  return roundMoney(Number(monthlyBudget) - Number(spent));
}

export function spentShare(monthlyBudget, spent) {
  const budget = Number(monthlyBudget);
  const used = Number(spent);
  if (budget <= 0) return used > 0 ? 100 : 0;
  return Math.min(100, Math.max(0, (used / budget) * 100));
}

export function formatMonth(year, month) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, month - 1, 1)));
}

export function formatLongDate(iso) {
  if (!iso) return '';
  const [year, month, day] = iso.split('-').map(Number);
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, month - 1, day)));
}

export function shiftMonth(year, month, delta) {
  const date = new Date(Date.UTC(year, month - 1 + delta, 1));
  return { year: date.getUTCFullYear(), month: date.getUTCMonth() + 1 };
}

export function isoDate(date) {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function defaultSpentOn(year, month, today = new Date()) {
  if (today.getFullYear() === year && today.getMonth() + 1 === month) {
    return isoDate(today);
  }
  return `${year}-${String(month).padStart(2, '0')}-01`;
}
