export interface Money {
  amount: number;
  currencyCode: string;
}

export function money(amount: number, currencyCode: string): Money {
  if (!Number.isFinite(amount) || amount < 0) {
    throw new Error('amount_invalid');
  }
  if (!currencyCode || currencyCode.length !== 3) {
    throw new Error('currency_invalid');
  }
  return { amount: Math.round(amount * 100) / 100, currencyCode };
}
