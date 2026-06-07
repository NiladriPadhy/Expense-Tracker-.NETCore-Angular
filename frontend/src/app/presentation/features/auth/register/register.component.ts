import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthFacade } from '../../../../application/auth/auth.facade';
import { Currency } from '../../../../domain/models';
import { CurrenciesRepository } from '../../../../domain/ports/currencies.repository';
import { extractError } from '../../../shared/problem-details';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-shell">
      <div class="auth-card">
        <div class="auth-brand">
          <span class="auth-brand__mark" aria-hidden="true">₹</span>
          <span class="auth-brand__text">ExpenseTracker</span>
        </div>
        <h2 class="auth-title">Create your account</h2>
        <p class="auth-subtitle">Set up your profile to start tracking income and expenses.</p>

        <form (ngSubmit)="submit()" #f="ngForm" novalidate class="auth-form">
          <label>Name <input name="name" [(ngModel)]="name" required maxlength="100" autocomplete="name" /></label>
          <div class="grid-2">
            <label>Email <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" /></label>
            <label>Phone <small class="hint">E.164 (e.g. +14155550100)</small>
              <input name="phone" [(ngModel)]="phone" required [pattern]="phonePattern" autocomplete="tel" />
            </label>
          </div>
          <label>Password <small class="hint">Min 8 chars, include a letter and a digit.</small>
            <input name="pw" type="password" [(ngModel)]="password" required minlength="8" autocomplete="new-password" />
          </label>
          <label>Preferred currency
            <select name="cur" [(ngModel)]="currencyCode" required>
              @for (c of currencies(); track c.code) { <option [value]="c.code">{{ c.code }} — {{ c.name }}</option> }
            </select>
          </label>
          <label>Profile photo <small class="hint">Optional. JPG/PNG/WEBP, max 2 MB.</small>
            <input name="photo" type="file" (change)="onFile($event)" accept="image/jpeg,image/png,image/webp" />
          </label>

          @if (error()) {
            <div class="alert alert-danger">
              @if (fieldErrors()) {
                <ul>
                  @for (entry of fieldErrorRows(); track entry.field) {
                    <li><strong>{{ entry.field }}:</strong> {{ entry.message }}</li>
                  }
                </ul>
              } @else {
                {{ error() }}
              }
            </div>
          }

          <button type="submit" [disabled]="busy() || !f.valid">
            {{ busy() ? 'Creating…' : 'Create account' }}
          </button>
        </form>

        <p class="auth-foot">Already registered? <a routerLink="/login">Sign in</a></p>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .auth-shell {
      min-height: calc(100vh - 0px);
      display: grid; place-items: center;
      padding: 2rem 1rem;
    }
    .auth-card {
      width: 100%; max-width: 520px;
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-lg);
      padding: 2rem 1.75rem;
    }
    .auth-brand { display:inline-flex; align-items:center; gap:.55rem; margin-bottom:1.25rem; }
    .auth-brand__mark {
      display:inline-flex; align-items:center; justify-content:center;
      width: 36px; height: 36px;
      background: linear-gradient(135deg, var(--color-primary), #7c3aed);
      color:#fff; font-weight:700; border-radius: var(--radius-md);
      box-shadow: 0 6px 18px rgba(79, 70, 229, .3);
    }
    .auth-brand__text { font-weight: 700; letter-spacing: -0.01em; }
    .auth-title { margin: 0 0 .25rem; font-size: 1.5rem; }
    .auth-subtitle { margin: 0 0 1.25rem; color: var(--color-text-muted); }
    .auth-form { display: grid; gap: .85rem; }
    .auth-form button[type="submit"] { padding: .65rem 1rem; font-weight: 600; }
    .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: .85rem; }
    .hint { font-weight: 400; color: var(--color-text-subtle); font-size: .75rem; }
    .alert ul { margin: 0; padding-left: 1.1rem; }
    .alert li { line-height: 1.4; }
    .auth-foot { margin-top: 1rem; color: var(--color-text-muted); font-size: .9rem; text-align: center; }
    @media (max-width: 540px) { .grid-2 { grid-template-columns: 1fr; } }
  `],
})
export class RegisterComponent implements OnInit {
  protected readonly phonePattern = '^\\+[1-9]\\d{7,14}$';
  protected name = '';
  protected email = '';
  protected phone = '';
  protected password = '';
  protected currencyCode = 'USD';
  protected photo: File | null = null;
  protected currencies = signal<Currency[]>([]);
  protected busy = signal(false);
  protected error = signal<string | null>(null);
  protected fieldErrors = signal<Record<string, string[]> | null>(null);
  protected fieldErrorRows = signal<{ field: string; message: string }[]>([]);

  private readonly currenciesRepo = inject(CurrenciesRepository);
  private readonly auth = inject(AuthFacade);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.currenciesRepo.listActive().subscribe({ next: (list) => this.currencies.set(list) });
  }

  protected onFile(ev: Event): void {
    const f = (ev.target as HTMLInputElement).files?.[0] ?? null;
    if (f && f.size > 2 * 1024 * 1024) { this.error.set('photo_too_large'); return; }
    this.photo = f;
  }

  protected submit(): void {
    this.busy.set(true);
    this.error.set(null);
    this.fieldErrors.set(null);
    this.fieldErrorRows.set([]);
    this.auth.register(
      { fullName: this.name, email: this.email, phone: this.phone, password: this.password, currencyCode: this.currencyCode },
      this.photo ?? undefined,
    ).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e: unknown) => {
        const info = extractError(e);
        this.error.set(info.message);
        if (info.fieldErrors) {
          this.fieldErrors.set(info.fieldErrors);
          const rows: { field: string; message: string }[] = [];
          for (const [field, msgs] of Object.entries(info.fieldErrors)) {
            for (const m of msgs) rows.push({ field, message: m });
          }
          this.fieldErrorRows.set(rows);
        }
        this.busy.set(false);
      },
      complete: () => this.busy.set(false),
    });
  }
}
