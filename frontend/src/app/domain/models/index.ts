export type UserRole = 'User' | 'Admin';
export type EntryType = 'Expense' | 'Income';
export type StatusColor = 'Green' | 'Orange' | 'OrangeRedTint' | 'BloodRed';

/** Mirrors backend `UserProfileDto`. */
export interface User {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  currencyCode: string;
  role: UserRole;
  hasPhoto: boolean;
}

/** Mirrors backend `AdminUserDto`. */
export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  currencyCode: string;
  role: UserRole;
  isActive: boolean;
  isDeleted: boolean;
  hasPhoto: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Currency {
  code: string;
  name: string;
  symbol: string;
  isActive: boolean;
}

export interface Category {
  id: string;
  name: string;
  type: EntryType;
  isActive: boolean;
}

/** Mirrors backend `EntryDto`. */
export interface Entry {
  id: string;
  entryDate: string; // ISO date YYYY-MM-DD
  type: EntryType;
  amount: number;
  currencyCode: string;
  categoryId: string | null;
  categoryName: string;
  note: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

/** Mirrors backend `MonthlyViewDto`. */
export interface MonthlyView {
  year: number;
  month: number;
  currencyCode: string;
  openingBalance: number;
  totalIncome: number;
  totalExpense: number;
  closingBalance: number;
  savingsRatePct: number;
  statusColor: StatusColor;
  readOnly: boolean;
  entries: Entry[];
}

/** Mirrors backend `DashboardMonthPointDto`. */
export interface DashboardMonthPoint {
  year: number;
  month: number;
  totalIncome: number;
  totalExpense: number;
  savings: number;
  savingsRatePct: number;
  statusColor: StatusColor;
}

/** Mirrors backend `DashboardDto`. */
export interface Dashboard {
  currencyCode: string;
  currentMonthSavingsRatePct: number;
  currentMonthStatusColor: StatusColor;
  alertExpenseExceedsIncome: boolean;
  trend: DashboardMonthPoint[];
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: User;
}
