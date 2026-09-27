import { useEffect, useId, useMemo, useRef, useState } from 'react';
import gsap from 'gsap';
import { formatLongDate, formatMoney, formatMonthYear } from '../../lib/money';
import { buildSchedule, calculatePayoff, yearlyBuckets } from '../../lib/payoff';

const WIDTH = 640;
const HEIGHT = 220;
const TYPE_LABELS = ['Revolving facility', 'Other installment loan', 'Home loan', 'Car loan', 'Credit card'];

export function DebtPanel({ facility, pending, onApply, onDelete, onEdit }) {
  const schedule = facility.schedule ?? [];
  const [extraMonthly, setExtraMonthly] = useState('');
  const [oneOff, setOneOff] = useState('');
  const [showAll, setShowAll] = useState(false);
  const typeLabel = TYPE_LABELS[Number(facility.type)] ?? (facility.kind === 1 ? 'Installment loan' : 'Revolving');
  const asOf = new Date().toISOString().slice(0, 10);
  const extraMonthlyValue = Math.max(0, Number(extraMonthly) || 0);
  const oneOffValue = Math.max(0, Number(oneOff) || 0);
  const hasExtra = extraMonthlyValue > 0 || oneOffValue > 0;

  const scenario = useMemo(() => calculatePayoff({
    balance: facility.balance,
    annualInterestPercent: facility.annualInterestRate,
    monthlyPayment: facility.monthlyPayment,
    monthlyAdminFee: facility.monthlyAdminFee,
    extraMonthlyPayment: extraMonthlyValue,
    oneOffPayment: oneOffValue,
    asOf,
  }), [facility, extraMonthlyValue, oneOffValue, asOf]);

  const scenarioSchedule = useMemo(() => buildSchedule({
    balance: facility.balance,
    annualInterestPercent: facility.annualInterestRate,
    monthlyPayment: facility.monthlyPayment,
    monthlyAdminFee: facility.monthlyAdminFee,
    extraMonthlyPayment: extraMonthlyValue,
    oneOffPayment: oneOffValue,
    termMonths: facility.termMonths,
    asOf,
  }), [facility, extraMonthlyValue, oneOffValue, asOf]);

  const visibleSchedule = hasExtra ? scenarioSchedule : schedule;
  const visibleRows = showAll ? visibleSchedule : visibleSchedule.slice(0, 12);
  const baselinePaysOff = Boolean(facility.willPayOff);
  const baselineCost = facility.totalInterest == null
    ? null
    : Number(facility.totalInterest) + Number(facility.totalFees ?? 0);
  const scenarioCost = scenario.totalInterest == null
    ? null
    : Number(scenario.totalInterest) + Number(scenario.totalFees ?? 0);
  const monthsSaved = baselinePaysOff && scenario.willPayOff
    ? Number(facility.monthsToPayoff) - Number(scenario.months)
    : null;
  const costSaved = baselineCost != null && scenarioCost != null ? baselineCost - scenarioCost : null;
  const cleared = schedule.length > 0 && schedule[schedule.length - 1].balance === 0;

  return (
    <article className="debt">
      <header className="debt-head">
        <div>
          <h3>{facility.name}</h3>
          <p>
            {typeLabel} · {facility.personName} pays this · due day {facility.dueDay}
            {facility.termMonths ? ` · ${facility.termMonths} months remaining` : ''}
          </p>
        </div>
        <div className="debt-balance">
          <span>Current balance</span>
          <strong>{formatMoney(facility.balance)}</strong>
        </div>
      </header>

      <section className="payoff-simulator" aria-label={`${facility.name} extra payment comparison`}>
        <div className="simulator-copy">
          <h4>Try extra payments</h4>
          <p>See how faster payments change payoff time and total charges.</p>
        </div>
        <div className="simulator-inputs">
          <label>
            Extra each month
            <span className="money-input"><span aria-hidden="true">+</span><input aria-label={`${facility.name} extra each month`} type="number" min="0" step="0.01" value={extraMonthly} onChange={(event) => setExtraMonthly(event.target.value)} /></span>
          </label>
          <label>
            One-off payment now
            <span className="money-input"><span aria-hidden="true">+</span><input aria-label={`${facility.name} one-off payment now`} type="number" min="0" step="0.01" value={oneOff} onChange={(event) => setOneOff(event.target.value)} /></span>
          </label>
        </div>
        <div className="comparison-stats">
          <Comparison label="Current payoff" value={baselinePaysOff ? formatLongDate(facility.payoffDate) : 'Does not clear'} />
          <Comparison label="With extras" value={scenario.willPayOff ? formatLongDate(scenario.payoffDate) : 'Does not clear'} tone={scenario.willPayOff ? 'good' : 'bad'} />
          <Comparison label="Time saved" value={monthsSaved != null ? `${Math.max(0, monthsSaved)} months` : (!baselinePaysOff && scenario.willPayOff ? 'Now clears' : '—')} />
          <Comparison label="Interest and fees saved" value={costSaved != null ? formatMoney(Math.max(0, costSaved)) : '—'} />
        </div>
      </section>

      <div className="debt-charts">
        <BalanceChart
          opening={facility.balance}
          baselineSchedule={schedule}
          scenarioOpening={Math.max(0, Number(facility.balance) - oneOffValue)}
          scenarioSchedule={scenarioSchedule}
          hasExtra={hasExtra}
          label={`${facility.name} balance comparison`}
        />
        <SplitChart schedule={visibleSchedule} label={`${facility.name} interest, fees, and principal`} />
      </div>

      <p className="meta debt-assumptions">
        {facility.annualInterestRate}% a year · {facility.kind === 1 ? 'payment' : 'minimum payment'} {formatMoney(facility.monthlyPayment)}
        {Number(facility.monthlyAdminFee) > 0 ? ` · monthly admin fee ${formatMoney(facility.monthlyAdminFee)}` : ''}
        {facility.totalInterest != null ? ` · current total interest ${formatMoney(facility.totalInterest)} · fees ${formatMoney(facility.totalFees ?? 0)}` : ''}
      </p>

      <div className="schedule-wrap">
        <table className="schedule">
          <caption>{hasExtra ? 'Scenario payment schedule' : 'Current payment schedule'}</caption>
          <thead>
            <tr>
              <th scope="col">Month</th>
              <th scope="col">Date</th>
              <th scope="col">Payment</th>
              <th scope="col">Interest</th>
              <th scope="col">Fees</th>
              <th scope="col">Principal</th>
              <th scope="col">Balance</th>
            </tr>
          </thead>
          <tbody>
            {hasExtra && oneOffValue > 0 && (
              <tr>
                <td>Now</td>
                <td>{formatMonthYear(asOf)}</td>
                <td>{formatMoney(Math.min(Number(facility.balance), oneOffValue))}</td>
                <td>{formatMoney(0)}</td>
                <td>{formatMoney(0)}</td>
                <td>{formatMoney(Math.min(Number(facility.balance), oneOffValue))}</td>
                <td>{formatMoney(Math.max(0, Number(facility.balance) - oneOffValue))}</td>
              </tr>
            )}
            {visibleRows.map((row) => (
              <tr key={row.month} className={row.balance === 0 ? 'cleared-row' : undefined}>
                <td>{row.month}</td>
                <td>{formatMonthYear(row.date)}</td>
                <td>{formatMoney(row.payment)}</td>
                <td>{formatMoney(row.interest)}</td>
                <td>{formatMoney(row.fee ?? 0)}</td>
                <td>{formatMoney(row.principal)}</td>
                <td>{formatMoney(row.balance)}</td>
              </tr>
            ))}
            {visibleRows.length === 0 && <tr><td colSpan="7">Paid off.</td></tr>}
          </tbody>
        </table>
        {visibleSchedule.length > 12 && (
          <button type="button" className="secondary" onClick={() => setShowAll((open) => !open)}>
            {showAll ? 'Show the first year' : `Show all ${visibleSchedule.length} months`}
          </button>
        )}
      </div>

      <div className="row-actions debt-actions">
        <button type="button" disabled={pending || facility.balance <= 0} onClick={onApply}>Apply required payment</button>
        <button type="button" className="secondary" disabled={pending} onClick={onEdit}>Edit details</button>
        <button type="button" className="quiet" disabled={pending} onClick={onDelete}>Remove</button>
      </div>
    </article>
  );
}

function Comparison({ label, value, tone }) {
  return <div className={tone ? `comparison-stat ${tone}` : 'comparison-stat'}><span>{label}</span><strong>{value}</strong></div>;
}

function BalanceChart({ opening, baselineSchedule, scenarioOpening, scenarioSchedule, hasExtra, label }) {
  const pathRef = useRef(null);
  const gradientId = useId().replace(/:/g, '');
  const { baseline, scenario } = useMemo(() => {
    const maxMonth = Math.max(1, baselineSchedule.at(-1)?.month ?? 0, scenarioSchedule.at(-1)?.month ?? 0);
    const maxBalance = Math.max(Number(opening), ...baselineSchedule.map((row) => Number(row.balance)), ...scenarioSchedule.map((row) => Number(row.balance)), 1);
    const project = (start, schedule) => [{ month: 0, balance: Number(start) }, ...schedule.map((row) => ({ month: row.month, balance: Number(row.balance) }))]
      .map((point) => ({
        x: 36 + (point.month / maxMonth) * (WIDTH - 52),
        y: 18 + (1 - point.balance / maxBalance) * (HEIGHT - 46),
        ...point,
      }));
    return {
      baseline: project(opening, baselineSchedule),
      scenario: project(scenarioOpening, scenarioSchedule),
    };
  }, [opening, baselineSchedule, scenarioOpening, scenarioSchedule]);

  const baselineLine = pathFor(baseline);
  const scenarioLine = pathFor(scenario);
  const scenarioArea = `${scenarioLine} L ${scenario.at(-1).x.toFixed(1)} ${HEIGHT - 28} L ${scenario[0].x.toFixed(1)} ${HEIGHT - 28} Z`;

  useEffect(() => {
    const path = pathRef.current;
    if (!path) return undefined;
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce) return undefined;
    const length = path.getTotalLength();
    const tween = gsap.fromTo(path, { strokeDasharray: length, strokeDashoffset: length }, {
      strokeDashoffset: 0,
      duration: 1.05,
      ease: 'power3.out',
    });
    return () => tween.kill();
  }, [scenarioLine]);

  return (
    <figure className="chart">
      <figcaption>Projected balance</figcaption>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="img" aria-label={label}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#1d6b45" stopOpacity="0.22" />
            <stop offset="100%" stopColor="#1d6b45" stopOpacity="0" />
          </linearGradient>
        </defs>
        {hasExtra && <path d={scenarioArea} fill={`url(#${gradientId})`} />}
        <path d={baselineLine} fill="none" stroke="#8c3a1e" strokeWidth="2" strokeDasharray={hasExtra ? '5 5' : undefined} strokeLinejoin="round" strokeLinecap="round" />
        <path ref={pathRef} d={scenarioLine} fill="none" stroke="#1d6b45" strokeWidth="2.8" strokeLinejoin="round" strokeLinecap="round" />
        <g transform={`translate(${scenario.at(-1).x} ${scenario.at(-1).y})`}>
          <circle r="5" fill="#1d6b45" />
          <circle r="10" fill="none" stroke="#1d6b45" strokeWidth="1.4" />
        </g>
      </svg>
      <p className="chart-key">
        <span className="swatch interest" /> Current payments
        {hasExtra && <><span className="swatch principal" /> With extras</>}
      </p>
    </figure>
  );
}

function pathFor(points) {
  return points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(1)} ${point.y.toFixed(1)}`).join(' ');
}

function SplitChart({ schedule, label }) {
  const years = yearlyBuckets(schedule);
  if (years.length === 0) return null;
  const max = Math.max(...years.map((year) => Math.abs(year.interest) + Math.abs(year.fees) + Math.abs(year.principal)), 1);
  const slot = (WIDTH - 48) / years.length;

  return (
    <figure className="chart">
      <figcaption>Interest, fees, and principal by year</figcaption>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="img" aria-label={label}>
        {years.map((year, index) => {
          const interestHeight = (Math.abs(year.interest) / max) * (HEIGHT - 52);
          const feesHeight = (Math.abs(year.fees) / max) * (HEIGHT - 52);
          const principalHeight = (Math.max(year.principal, 0) / max) * (HEIGHT - 52);
          const x = 28 + index * slot + slot * 0.22;
          const barWidth = Math.max(4, slot * 0.5);
          const base = HEIGHT - 28;
          return (
            <g key={year.label}>
              <rect x={x} y={base - principalHeight - feesHeight - interestHeight} width={barWidth} height={interestHeight} fill="#8c3a1e" rx="2" />
              <rect x={x} y={base - principalHeight - feesHeight} width={barWidth} height={feesHeight} fill="#d49a42" rx="2" />
              <rect x={x} y={base - principalHeight} width={barWidth} height={principalHeight} fill="#1d6b45" rx="2" />
              {(years.length <= 16 || index === 0 || index === years.length - 1) && (
                <text x={x + barWidth / 2} y={HEIGHT - 10} textAnchor="middle" fontSize="11" fill="#6b5344">{year.label}</text>
              )}
            </g>
          );
        })}
      </svg>
      <p className="chart-key">
        <span className="swatch principal" /> Principal
        <span className="swatch interest" /> Interest
        <span className="swatch fees" /> Fees
      </p>
    </figure>
  );
}
