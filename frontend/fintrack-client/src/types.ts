export type TransactionType = 'Income' | 'Expense'
export type CategoryType = 'Income' | 'Expense'
export type RecurrenceFrequency = 'Weekly' | 'Monthly' | 'Yearly'
export type BudgetStatus = 'Ok' | 'Warning' | 'Exceeded'

export interface UserDto {
  id: string
  email: string
  displayName: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
  user: UserDto
}

export interface Category {
  id: string
  name: string
  type: CategoryType
  isDefault: boolean
  isArchived: boolean
}

export interface Transaction {
  id: string
  type: TransactionType
  amount: number
  currency: string
  description?: string | null
  categoryId: string
  categoryName: string
  transactionDate: string
  createdAt: string
  updatedAt: string
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface Budget {
  id: string
  categoryId: string
  categoryName: string
  year: number
  month: number
  monthlyLimit: number
  spent: number
  remaining: number
  usagePercentage: number
  status: BudgetStatus
  isThresholdReached: boolean
}

export interface RecurringTransaction {
  id: string
  type: TransactionType
  amount: number
  currency: string
  categoryId: string
  categoryName: string
  description?: string | null
  frequency: RecurrenceFrequency
  startDate: string
  nextExecutionDate: string
  endDate?: string | null
  lastExecutedDate?: string | null
  isActive: boolean
}

export interface CategoryBreakdown {
  categoryName: string
  amount: number
  percentage: number
}

export interface DailyPoint {
  date: string
  amount: number
}

export interface RecentTransaction {
  id: string
  type: TransactionType
  amount: number
  categoryName: string
  transactionDate: string
  description?: string | null
}

export interface BudgetStatusItem {
  categoryName: string
  limit: number
  spent: number
  remaining: number
  usagePercentage: number
  status: BudgetStatus
}

export interface UpcomingPayment {
  description?: string | null
  amount: number
  categoryName: string
  nextExecutionDate: string
  frequency: RecurrenceFrequency
}

export interface Dashboard {
  year: number
  month: number
  totalIncome: number
  totalExpense: number
  netBalance: number
  incomeChangePercent: number | null
  expenseChangePercent: number | null
  expenseByCategory: CategoryBreakdown[]
  topExpenseCategories: CategoryBreakdown[]
  dailySpendingTrend: DailyPoint[]
  recentTransactions: RecentTransaction[]
  budgets: BudgetStatusItem[]
  upcomingPayments: UpcomingPayment[]
}

export type MessageRole = 'User' | 'Assistant'

export interface SourceRef {
  type: string
  category?: string | null
}

export interface DataPeriod {
  start: string
  end: string
}

export interface ChatResponse {
  answer: string
  conversationId: string
  dataPeriod: DataPeriod
  sources: SourceRef[]
}

export interface ConversationSummary {
  id: string
  title: string
  createdAt: string
}

export interface ChatMessage {
  role: MessageRole
  content: string
  createdAt: string
}

export interface ConversationDetail {
  id: string
  title: string
  createdAt: string
  messages: ChatMessage[]
}
