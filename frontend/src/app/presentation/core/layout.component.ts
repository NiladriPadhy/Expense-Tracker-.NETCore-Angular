import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthFacade } from '../../application/auth/auth.facade';
import { AuthRepository } from '../../domain/ports/auth.repository';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <div class="topbar__inner">
        <a class="brand" routerLink="/">
          <span class="brand__mark" aria-hidden="true">₹</span>
          <span class="brand__text">ExpenseTracker</span>
        </a>

        @if (auth.isAuthenticated()) {
          <nav class="nav" aria-label="Primary">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Dashboard</a>
            <a routerLink="/months" routerLinkActive="active">Months</a>
            @if (auth.isAdmin()) {
              <span class="nav__divider" aria-hidden="true"></span>
              <a routerLink="/admin/users" routerLinkActive="active">Users</a>
              <a routerLink="/admin/categories" routerLinkActive="active">Categories</a>
              <a routerLink="/admin/currencies" routerLinkActive="active">Currencies</a>
            }
          </nav>

          <div class="user">
            @if (auth.isAdmin()) {
              <span class="badge badge-primary" title="Administrator">Admin</span>
            }
            <a routerLink="/profile" class="user__chip" title="Profile">
              @if (photoUrl()) {
                <img [src]="photoUrl()" class="avatar" alt="" />
              } @else {
                <span class="avatar avatar--initials" aria-hidden="true">{{ initials() }}</span>
              }
              <span class="user__name">{{ auth.user()?.fullName }}</span>
            </a>
            <button type="button" class="btn-ghost" (click)="logout()">Sign out</button>
          </div>
        }
      </div>
    </header>
    <main class="content"><router-outlet /></main>
  `,
  styles: [`
    :host { display: block; min-height: 100%; }

    .topbar {
      position: sticky;
      top: 0;
      z-index: 30;
      background: rgba(255, 255, 255, 0.85);
      backdrop-filter: saturate(180%) blur(12px);
      -webkit-backdrop-filter: saturate(180%) blur(12px);
      border-bottom: 1px solid var(--color-border);
    }
    .topbar__inner {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      gap: 1.25rem;
      padding: 0.65rem 1.25rem;
    }

    .brand { display:inline-flex; align-items:center; gap:.55rem; text-decoration:none; color: var(--color-text); font-weight:700; }
    .brand:hover { text-decoration: none; }
    .brand__mark {
      display:inline-flex; align-items:center; justify-content:center;
      width: 30px; height: 30px;
      background: linear-gradient(135deg, var(--color-primary), #7c3aed);
      color: #fff; font-weight: 700; font-family: var(--font-mono);
      border-radius: var(--radius-md);
      box-shadow: 0 4px 12px rgba(79, 70, 229, .3);
    }
    .brand__text { letter-spacing: -0.01em; }

    .nav {
      display: flex; align-items: center; gap: .35rem;
      flex: 1;
      margin-left: .5rem;
    }
    .nav a {
      position: relative;
      text-decoration: none;
      color: var(--color-text-muted);
      padding: .4rem .75rem;
      border-radius: var(--radius-sm);
      font-weight: 500;
      transition: color var(--transition-fast), background var(--transition-fast);
    }
    .nav a:hover { color: var(--color-text); background: var(--color-surface-2); }
    .nav a.active {
      background: var(--color-primary-soft);
      color: var(--color-primary-strong);
    }
    .nav__divider {
      width: 1px; height: 20px; background: var(--color-border);
      margin: 0 .35rem;
    }

    .user { display:flex; align-items:center; gap:.55rem; }
    .user__chip {
      display:inline-flex; align-items:center; gap:.45rem;
      padding: .25rem .55rem .25rem .25rem;
      border-radius: var(--radius-pill);
      background: var(--color-surface-2);
      border: 1px solid var(--color-border);
      color: var(--color-text);
      text-decoration: none;
      transition: background var(--transition-fast), border-color var(--transition-fast);
    }
    .user__chip:hover { background: #fff; border-color: var(--color-border-strong); text-decoration: none; }
    .user__name { font-weight: 500; font-size: .88rem; }
    .avatar {
      width: 28px; height: 28px;
      border-radius: 50%;
      object-fit: cover;
      background: var(--color-primary-soft);
      flex-shrink: 0;
    }
    .avatar--initials {
      display:inline-flex; align-items:center; justify-content:center;
      font-size: .72rem; font-weight: 600;
      color: var(--color-primary-strong);
      background: var(--color-primary-soft);
    }

    .content { max-width: 1200px; margin: 0 auto; padding: 1.5rem 1.25rem 3rem; }

    @media (max-width: 768px) {
      .topbar__inner { flex-wrap: wrap; gap: .5rem; padding: .5rem .75rem; }
      .nav { order: 3; flex-basis: 100%; overflow-x: auto; padding-bottom: .25rem; }
      .user { margin-left: auto; }
      .user__name { display: none; }
      .content { padding: 1rem .75rem 2rem; }
    }
  `],
})
export class LayoutComponent {
  protected auth = inject(AuthFacade);
  private readonly repo = inject(AuthRepository);
  private readonly router = inject(Router);

  protected photoUrl = computed(() => {
    const u = this.auth.user();
    return u?.hasPhoto ? this.repo.photoUrl(u.id) : null;
  });

  protected initials = computed(() => {
    const name = this.auth.user()?.fullName ?? '';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts.at(-1)?.[0] ?? '' : '';
    return (first + last).toUpperCase();
  });

  protected logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
