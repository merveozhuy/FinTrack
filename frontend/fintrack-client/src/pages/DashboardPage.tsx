import { useState } from 'react'
import {
  Bar, BarChart, CartesianGrid, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis,
} from 'recharts'
import { useDashboard } from '../api/dashboard'
import { Badge, ErrorState, PageHeader, Spinner, EmptyState } from '../components/ui'
import { formatCurrency, formatDate, formatPercent, MONTH_NAMES } from '../lib/format'
import type { BudgetStatus } from '../types'

const now = new Date()
const PIE_COLORS = ['#4f46e5', '#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6']

const budgetTone: Record<BudgetStatus, 'emerald' | 'amber' | 'rose'> = {
  Ok: 'emerald',
  Warning: 'amber',
  Exceeded: 'rose',
}

function StatCard({ label, value, change }: { label: string; value: string; change?: number | null }) {
  return (
    <div className="card">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-slate-900">{value}</p>
      {change !== undefined && (
        <p className={`mt-1 text-xs ${change !== null && change < 0 ? 'text-emerald-600' : 'text-slate-400'}`}>
          {formatPercent(change ?? null)} vs last month
        </p>
      )}
    </div>
  )
}

export function DashboardPage() {
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const { data, isLoading, isError } = useDashboard(year, month)

  return (
    <div>
      <PageHeader
        title="Dashboard"
        subtitle="Your monthly financial overview"
        action={
          <div className="flex gap-2">
            <select className="input w-40" value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {MONTH_NAMES.map((name, index) => (
                <option key={name} value={index + 1}>{name}</option>
              ))}
            </select>
            <select className="input w-28" value={year} onChange={(e) => setYear(Number(e.target.value))}>
              {[now.getFullYear(), now.getFullYear() - 1, now.getFullYear() - 2].map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
        }
      />

      {isLoading && <Spinner label="Loading dashboard…" />}
      {isError && <ErrorState message="Could not load the dashboard." />}

      {data && (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Total income" value={formatCurrency(data.totalIncome)} change={data.incomeChangePercent} />
            <StatCard label="Total expense" value={formatCurrency(data.totalExpense)} change={data.expenseChangePercent} />
            <StatCard label="Net balance" value={formatCurrency(data.netBalance)} />
            <StatCard label="Budgets tracked" value={String(data.budgets.length)} />
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Expense by category</h3>
              {data.expenseByCategory.length === 0 ? (
                <EmptyState title="No expenses this month" />
              ) : (
                <ResponsiveContainer width="100%" height={260}>
                  <PieChart>
                    <Pie
                      data={data.expenseByCategory}
                      dataKey="amount"
                      nameKey="categoryName"
                      innerRadius={60}
                      outerRadius={95}
                      paddingAngle={2}
                    >
                      {data.expenseByCategory.map((entry, index) => (
                        <Cell key={entry.categoryName} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => formatCurrency(Number(value))} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              )}
            </div>

            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Daily spending</h3>
              {data.dailySpendingTrend.length === 0 ? (
                <EmptyState title="No spending recorded" />
              ) : (
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={data.dailySpendingTrend}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="date" tickFormatter={(value) => String(value).slice(-2)} fontSize={12} />
                    <YAxis fontSize={12} width={70} tickFormatter={(value) => formatCurrency(Number(value))} />
                    <Tooltip formatter={(value) => formatCurrency(Number(value))} labelFormatter={(label) => formatDate(String(label))} />
                    <Bar dataKey="amount" fill="#4f46e5" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Recent transactions</h3>
              {data.recentTransactions.length === 0 ? (
                <EmptyState title="No transactions yet" />
              ) : (
                <ul className="divide-y divide-slate-100">
                  {data.recentTransactions.map((t) => (
                    <li key={t.id} className="flex items-center justify-between py-2.5">
                      <div>
                        <p className="text-sm font-medium text-slate-800">{t.categoryName}</p>
                        <p className="text-xs text-slate-400">{formatDate(t.transactionDate)}{t.description ? ` · ${t.description}` : ''}</p>
                      </div>
                      <span className={`text-sm font-semibold ${t.type === 'Income' ? 'text-emerald-600' : 'text-slate-800'}`}>
                        {t.type === 'Income' ? '+' : '−'}{formatCurrency(t.amount)}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Budget status</h3>
              {data.budgets.length === 0 ? (
                <EmptyState title="No budgets for this month" hint="Add budgets from the Budgets page." />
              ) : (
                <ul className="space-y-3">
                  {data.budgets.map((b) => (
                    <li key={b.categoryName}>
                      <div className="mb-1 flex items-center justify-between text-sm">
                        <span className="font-medium text-slate-700">{b.categoryName}</span>
                        <Badge tone={budgetTone[b.status]}>{b.status}</Badge>
                      </div>
                      <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                        <div
                          className={`h-full ${b.status === 'Exceeded' ? 'bg-rose-500' : b.status === 'Warning' ? 'bg-amber-500' : 'bg-emerald-500'}`}
                          style={{ width: `${Math.min(100, b.usagePercentage)}%` }}
                        />
                      </div>
                      <p className="mt-1 text-xs text-slate-400">
                        {formatCurrency(b.spent)} of {formatCurrency(b.limit)} · {b.usagePercentage}%
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>

          {data.upcomingPayments.length > 0 && (
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Upcoming payments</h3>
              <ul className="divide-y divide-slate-100">
                {data.upcomingPayments.map((p, index) => (
                  <li key={index} className="flex items-center justify-between py-2.5 text-sm">
                    <span className="text-slate-700">{p.categoryName}{p.description ? ` · ${p.description}` : ''}</span>
                    <span className="text-slate-500">{formatDate(p.nextExecutionDate)} · {formatCurrency(p.amount)}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
