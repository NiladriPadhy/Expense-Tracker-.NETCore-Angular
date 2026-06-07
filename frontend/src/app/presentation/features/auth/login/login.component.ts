import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthFacade } from '../../../../application/auth/auth.facade';
import { extractError } from '../../../shared/problem-details';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-shell">
      <div class="auth-card">
        <div class="auth-brand">
          <span class="auth-brand__mark" aria-hidden="true">₹</span>
          <span class="auth-brand__text">ExpenseTracker</span>
        </div>
        <h2 class="auth-title">Welcome back</h2>
        <p class="auth-subtitle">Sign in to manage your monthly budget.</p>

        <form (ngSubmit)="submit()" #f="ngForm" novalidate class="auth-form">
          <label>
            Email or phone
            <input name="id" [(ngModel)]="identifier" required autocomplete="username" />
          </label>
          <label>
            Password
            <input name="pw" type="password" [(ngModel)]="password" required autocomplete="current-password" />
          </label>
          @if (error()) { <p class="alert alert-danger">{{ error() }}</p> }
          <button type="submit" [disabled]="busy() || !f.valid">
            {{ busy() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <p class="auth-foot">Need an account? <a routerLink="/register">Register</a></p>
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
      width: 100%; max-width: 420px;
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
    .auth-foot { margin-top: 1rem; color: var(--color-text-muted); font-size: .9rem; text-align: center; }
  `],
})
export class LoginComponent {
  protected identifier = '';
  protected password = '';
  protected busy = signal(false);
  protected error = signal<string | null>(null);
  private readonly auth = inject(AuthFacade);
  private readonly router = inject(Router);

  protected submit(): void {
    if (!this.identifier || !this.password) return;
    this.busy.set(true);
    this.error.set(null);
    this.auth.login({ identifier: this.identifier, password: this.password }).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e: unknown) => { this.error.set(extractError(e).message); this.busy.set(false); },
      complete: () => this.busy.set(false),
    });
  }
}
