import { Link } from 'react-router-dom';
import { useAuth } from './api-authorization/AuthContext';
import { BudgetPage } from './budget/BudgetPage';

export function Home() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading || !isAuthenticated) {
    return (
      <div>
        <h1>Welcome</h1>
        <p>Budge is a household ledger for people, the bills they pay, credit balances, and the budget left after spending.</p>
        <p><Link to="/login">Log in</Link> to open the ledger.</p>
      </div>
    );
  }

  return <BudgetPage />;
}
