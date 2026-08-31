import { useState } from 'react'
import {
  Bar, BarChart, CartesianGrid, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis,
} from 'recharts'
import { useDashboard } from '../api/dashboard'
import { Badge, EmptyState, ErrorState, PageHeader, Spinner } from '../components/ui'
import { formatCurrency, formatDate, formatPercent } from '../lib/format'
import { budgetStatusLabel, MONTH_NAMES_TR } from '../lib/labels'
import type { BudgetStatus } from '../types'

const now = new Date()
const PIE_COLORS = ['#059669', '#0ea5e9', '#f59e0b', '#8b5cf6', '#ef4444', '#14b8a6', '#ec4899', '#64748b']

const budgetTone: Record<BudgetStatus, 'emerald' | 'amber' | 'rose'> = {
  Ok: 'emerald',
  Warning: 'amber',
  Exceeded: 'rose',
}

function StatCard({ label, value, change, accent }: { label: string; value: string; change?: number | null; accent?: string }) {
  return (
    <div className="card">
      <p className="text-sm text-slate-500">{label}</p>
      <p className={`mt-1 text-2xl font-semibold ${accent ?? 'text-slate-900'}`}>{value}</p>
      {change !== undefined && (
        <p className="mt-1 text-xs text-slate-400">{formatPercent(change ?? null)} geçen aya göre</p>
      )}
    </div>
  )
}

export function DashboardPage() {
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const { data, isLoading, isError } = useDashboard(year, month)

  const savingsRate = data && data.totalIncome > 0
    ? Math.round((data.netBalance / data.totalIncome) * 1000) / 10
    : null

  return (
    <div>
      <PageHeader
        title="Panel"
        subtitle="Aylık finansal genel bakışınız"
        action={
          <div className="flex gap-2">
            <select className="input w-36" value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {MONTH_NAMES_TR.map((name, index) => (
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

      {isLoading && <Spinner label="Panel yükleniyor…" />}
      {isError && <ErrorState message="Panel yüklenemedi." />}

      {data && (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Toplam Gelir" value={formatCurrency(data.totalIncome)} change={data.incomeChangePercent} accent="text-emerald-600" />
            <StatCard label="Toplam Gider" value={formatCurrency(data.totalExpense)} change={data.expenseChangePercent} accent="text-rose-600" />
            <StatCard label="Net Bakiye" value={formatCurrency(data.netBalance)} accent={data.netBalance < 0 ? 'text-rose-600' : 'text-slate-900'} />
            <StatCard label="Tasarruf Oranı" value={savingsRate === null ? '—' : `%${savingsRate.toFixed(1)}`} />
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Kategoriye göre gider</h3>
              {data.expenseByCategory.length === 0 ? (
                <EmptyState title="Bu ay gider yok" />
              ) : (
                <ResponsiveContainer width="100%" height={260}>
                  <PieChart>
                    <Pie data={data.expenseByCategory} dataKey="amount" nameKey="categoryName" innerRadius={60} outerRadius={95} paddingAngle={2}>
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
              <h3 className="mb-4 font-semibold text-slate-900">Günlük harcama</h3>
              {data.dailySpendingTrend.length === 0 ? (
                <EmptyState title="Harcama kaydı yok" />
              ) : (
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={data.dailySpendingTrend}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="date" tickFormatter={(value) => String(value).slice(-2)} fontSize={12} />
                    <YAxis fontSize={12} width={70} tickFormatter={(value) => formatCurrency(Number(value))} />
                    <Tooltip formatter={(value) => formatCurrency(Number(value))} labelFormatter={(label) => formatDate(String(label))} />
                    <Bar dataKey="amount" fill="#059669" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Son işlemler</h3>
              {data.recentTransactions.length === 0 ? (
                <EmptyState title="Henüz işlem yok" />
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
              <h3 className="mb-4 font-semibold text-slate-900">Bütçe durumu</h3>
              {data.budgets.length === 0 ? (
                <EmptyState title="Bu ay bütçe yok" hint="Bütçeler sayfasından ekleyebilirsiniz." />
              ) : (
                <ul className="space-y-3">
                  {data.budgets.map((b) => (
                    <li key={b.categoryName}>
                      <div className="mb-1 flex items-center justify-between text-sm">
                        <span className="font-medium text-slate-700">{b.categoryName}</span>
                        <Badge tone={budgetTone[b.status]}>{budgetStatusLabel(b.status)}</Badge>
                      </div>
                      <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                        <div
                          className={`h-full ${b.status === 'Exceeded' ? 'bg-rose-500' : b.status === 'Warning' ? 'bg-amber-500' : 'bg-emerald-500'}`}
                          style={{ width: `${Math.min(100, b.usagePercentage)}%` }}
                        />
                      </div>
                      <p className="mt-1 text-xs text-slate-400">
                        {formatCurrency(b.spent)} / {formatCurrency(b.limit)} · %{b.usagePercentage}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>

          {data.upcomingPayments.length > 0 && (
            <div className="card">
              <h3 className="mb-4 font-semibold text-slate-900">Yaklaşan ödemeler</h3>
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
