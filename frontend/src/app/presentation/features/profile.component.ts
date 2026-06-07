import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthFacade } from '../../application/auth/auth.facade';
import { Currency, User } from '../../domain/models';
import { AuthRepository } from '../../domain/ports/auth.repository';
import { CurrenciesRepository } from '../../domain/ports/currencies.repository';
import { extractError } from '../shared/problem-details';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="page-head">
      <div>
        <h2>My profile</h2>
        <p class="muted">Update your personal info and avatar.</p>
      </div>
    </header>

    @let u = auth.user();
    @if (u) {
      <section class="profile card">
        <aside class="avatar-block">
          @if (photoUrl()) {
            <img [src]="photoUrl()" alt="avatar" class="avatar-img" />
          } @else {
            <div class="avatar-img placeholder" aria-hidden="true">{{ initials() }}</div>
          }
          <label class="upload">
            <span>Choose photo</span>
            <input type="file" (change)="onPhoto($event)" accept="image/jpeg,image/png,image/webp" />
          </label>
          <small class="muted">JPG/PNG/WEBP, max 2 MB</small>
          <button type="button" [disabled]="!pendingPhoto || busy()" (click)="uploadPhoto()">
            {{ pendingPhoto ? 'Upload selected' : 'No photo selected' }}
          </button>
        </aside>

        <form (ngSubmit)="save()" #f="ngForm" novalidate class="profile-form">
          <div class="grid-2">
            <label>Name <input name="name" [(ngModel)]="name" required /></label>
            <label>Email <small class="hint">(read-only)</small>
              <input name="email" type="email" [(ngModel)]="email" disabled />
            </label>
            <label>Phone <input name="phone" [(ngModel)]="phone" required /></label>
            <label>Preferred currency
              <select name="cur" [(ngModel)]="currencyCode" required>
                @for (c of currencies(); track c.code) { <option [value]="c.code">{{ c.code }} — {{ c.name }}</option> }
              </select>
            </label>
          </div>
          @if (msg()) { <div class="alert alert-success">{{ msg() }}</div> }
          @if (err()) { <div class="alert alert-danger">{{ err() }}</div> }
          <div class="row-flex">
            <button type="submit" [disabled]="!f.valid || busy()">
              {{ busy() ? 'Saving…' : 'Save changes' }}
            </button>
          </div>
        </form>
      </section>
    }
  `,
  styles: [`
    :host { display: block; }
    .page-head { margin-bottom: 1.25rem; }
    .page-head h2 { margin: 0; }
    .page-head .muted { margin: 0; }

    .profile {
      display: grid;
      grid-template-columns: 220px 1fr;
      gap: 2rem;
      align-items: start;
    }
    .avatar-block { display: grid; gap: .55rem; justify-items: start; }
    .avatar-img {
      width: 180px; height: 180px; border-radius: 50%; object-fit: cover;
      background: var(--color-primary-soft);
      box-shadow: var(--shadow-sm);
      border: 4px solid var(--color-surface);
    }
    .placeholder {
      display:inline-flex; align-items:center; justify-content:center;
      font-size: 2.5rem; font-weight: 600; color: var(--color-primary-strong);
    }
    .upload {
      cursor: pointer;
      display: inline-flex; align-items: center;
      padding: .35rem .65rem;
      background: var(--color-surface-2);
      border: 1px solid var(--color-border-strong);
      border-radius: var(--radius-sm);
      font-size: .85rem;
      gap: 0;
    }
    .upload input[type="file"] { display: none; }
    .upload:hover { background: #fff; }
    .avatar-block .muted { font-size: .75rem; }

    .profile-form { display: grid; gap: 1rem; }
    .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .hint { font-weight: 400; color: var(--color-text-subtle); font-size: .72rem; }

    @media (max-width: 720px) {
      .profile { grid-template-columns: 1fr; }
      .avatar-block { justify-items: center; }
      .grid-2 { grid-template-columns: 1fr; }
    }
  `],
})
export class ProfileComponent implements OnInit {
  protected name = '';
  protected email = '';
  protected phone = '';
  protected currencyCode = '';
  protected currencies = signal<Currency[]>([]);
  protected busy = signal(false);
  protected msg = signal<string | null>(null);
  protected err = signal<string | null>(null);
  protected pendingPhoto: File | null = null;

  protected auth = inject(AuthFacade);
  private readonly authRepo = inject(AuthRepository);
  private readonly currenciesRepo = inject(CurrenciesRepository);

  protected photoUrl = computed(() => {
    const u = this.auth.user();
    return u?.hasPhoto ? this.authRepo.photoUrl(u.id) : null;
  });

  protected initials = computed(() => {
    const name = this.auth.user()?.fullName ?? '';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts.at(-1)?.[0] ?? '' : '';
    return (first + last).toUpperCase();
  });

  ngOnInit(): void {
    const u: User | null = this.auth.user();
    if (u) {
      this.name = u.fullName; this.email = u.email; this.phone = u.phone; this.currencyCode = u.currencyCode;
    }
    this.currenciesRepo.listActive().subscribe({ next: (l: Currency[]) => this.currencies.set(l) });
  }

  protected onPhoto(ev: Event): void {
    const f = (ev.target as HTMLInputElement).files?.[0] ?? null;
    if (f && f.size > 2 * 1024 * 1024) { this.err.set('photo_too_large'); return; }
    this.pendingPhoto = f;
  }

  protected uploadPhoto(): void {
    if (!this.pendingPhoto) return;
    this.busy.set(true);
    this.authRepo.uploadPhoto(this.pendingPhoto).subscribe({
      next: () => {
        this.auth.refreshMe().subscribe({ next: () => { this.msg.set('Photo updated'); this.busy.set(false); this.pendingPhoto = null; } });
      },
      error: (e: unknown) => { this.err.set(extractError(e).message); this.busy.set(false); },
    });
  }

  protected save(): void {
    this.busy.set(true);
    this.err.set(null);
    this.msg.set(null);
    this.authRepo.updateMe({ fullName: this.name, phone: this.phone, currencyCode: this.currencyCode }).subscribe({
      next: () => { this.auth.refreshMe().subscribe({ next: () => { this.msg.set('Profile saved'); this.busy.set(false); } }); },
      error: (e: unknown) => { this.err.set(extractError(e).message); this.busy.set(false); },
    });
  }
}
