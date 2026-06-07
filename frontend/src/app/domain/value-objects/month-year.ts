export interface MonthYear {
  year: number;
  month: number;
}

export function monthYear(year: number, month: number): MonthYear {
  if (year < 1900 || year > 2200) throw new Error('year_out_of_range');
  if (month < 1 || month > 12) throw new Error('month_out_of_range');
  return { year, month };
}

export function toIsoMonth(m: MonthYear): string {
  return `${m.year.toString().padStart(4, '0')}-${m.month.toString().padStart(2, '0')}`;
}

export function fromIsoMonth(iso: string): MonthYear {
  const parts = iso.split('-');
  if (parts.length !== 2) throw new Error('iso_month_invalid');
  return monthYear(Number(parts[0]), Number(parts[1]));
}

export function currentMonth(now = new Date()): MonthYear {
  return monthYear(now.getUTCFullYear(), now.getUTCMonth() + 1);
}

export function isAfter(a: MonthYear, b: MonthYear): boolean {
  return a.year > b.year || (a.year === b.year && a.month > b.month);
}
