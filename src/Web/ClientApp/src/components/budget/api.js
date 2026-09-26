async function request(path, { method = 'GET', body } = {}) {
  const response = await fetch(path, {
    method,
    credentials: 'include',
    headers: body === undefined
      ? { Accept: 'application/json' }
      : { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  if (!response.ok) {
    const error = new Error(text || response.statusText);
    error.status = response.status;
    error.body = text;
    throw error;
  }

  return text ? JSON.parse(text) : null;
}

export function readProblem(error) {
  try {
    const body = JSON.parse(error.body ?? '');
    if (body.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    return body.detail || body.title || 'Something went wrong.';
  } catch {
    return 'Something went wrong.';
  }
}

export const getDashboard = (year, month) => request(`/api/Budget?year=${year}&month=${month}`);

export const createPerson = (name) => request('/api/People', { method: 'POST', body: { name } });

export const deletePerson = (id) => request(`/api/People/${id}`, { method: 'DELETE' });

export const createCategory = (body) => request('/api/Categories', { method: 'POST', body });

export const updateCategory = (id, body) => request(`/api/Categories/${id}`, { method: 'PUT', body });

export const deleteCategory = (id) => request(`/api/Categories/${id}`, { method: 'DELETE' });

export const createBill = (body) => request('/api/Bills', { method: 'POST', body });

export const deleteBill = (id) => request(`/api/Bills/${id}`, { method: 'DELETE' });

export const createCreditFacility = (body) => request('/api/CreditFacilities', { method: 'POST', body });

export const deleteCreditFacility = (id) => request(`/api/CreditFacilities/${id}`, { method: 'DELETE' });

export const applyCreditPayment = (id) => request(`/api/CreditFacilities/${id}/payments`, { method: 'POST' });

export const createExpense = (body) => request('/api/Expenses', { method: 'POST', body });

export const deleteExpense = (id) => request(`/api/Expenses/${id}`, { method: 'DELETE' });
