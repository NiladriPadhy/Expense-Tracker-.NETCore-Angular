import { StatusColor } from '../../domain/models';

/**
 * UI-only mirror of backend SavingsRateClassifier. Backend is the authority;
 * the API returns `statusColor` in MonthlySummary/MonthlyView/Dashboard.
 * This helper is for client-side previews where the backend value isn't yet available.
 */
export function classifySavingsRate(income: number, expense: number): StatusColor {
  if (income <= 0) return 'BloodRed';
  const ratePercent = ((income - expense) / income) * 100;
  if (ratePercent < 10) return 'BloodRed';
  if (ratePercent <= 20) return 'OrangeRedTint';
  if (ratePercent < 30) return 'Orange';
  return 'Green';
}

export const STATUS_COLOR_HEX: Record<StatusColor, string> = {
  Green: '#16a34a',
  Orange: '#f97316',
  OrangeRedTint: '#dc2626',
  BloodRed: '#7f1d1d',
};
