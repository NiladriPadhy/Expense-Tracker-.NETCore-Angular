/* RFC 7807 ProblemDetails helper */
import { HttpErrorResponse } from '@angular/common/http';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export function extractError(err: unknown): { code?: string; message: string; fieldErrors?: Record<string, string[]> } {
  if (err instanceof HttpErrorResponse) {
    const pd = (err.error ?? {}) as ProblemDetails;
    const fieldErrors = pd.errors;
    let message = pd.detail || pd.title || err.message;
    if (fieldErrors && Object.keys(fieldErrors).length > 0) {
      const lines: string[] = [];
      for (const [field, msgs] of Object.entries(fieldErrors)) {
        for (const m of msgs) lines.push(`${field}: ${m}`);
      }
      message = lines.join(' • ');
    }
    return { code: pd.code, message, fieldErrors };
  }
  return { message: 'Unknown error' };
}
