import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminUser, UserRole } from '../../../../domain/models';
import { AdminUsersRepository, PaginatedResult } from '../../../../domain/ports/admin.repository';
import { extractError } from '../../../shared/problem-details';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="page-head">
      <div>
        <h2>Manage users</h2>
        <p class="muted">{{ result()?.total ?? 0 }} total</p>
      </div>
      <div class="filters">
        <input placeholder="Search name, email or phone…" [(ngModel)]="search" (keyup.enter)="reload()" />
        <button type="button" (click)="reload()">Search</button>
      </div>
    </header>

    @if (err()) { <div class="alert alert-danger">{{ err() }}</div> }

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th>Name</th><th>Email</th><th>Phone</th>
            <th>Role</th><th>Active</th><th></th>
          </tr>
        </thead>
        <tbody>
          @for (u of result()?.items ?? []; track u.id) {
            <tr [class.deleted]="u.isDeleted">
              <td><strong>{{ u.fullName }}</strong></td>
              <td class="muted">{{ u.email }}</td>
              <td class="muted">{{ u.phone }}</td>
              <td>
                <select [ngModel]="u.role" (ngModelChange)="setRole(u, $event)" class="inline-select">
                  <option value="User">User</option>
                  <option value="Admin">Admin</option>
                </select>
              </td>
              <td>
                <label class="toggle">
                  <input type="checkbox" [checked]="u.isActive" (change)="setActive(u, $event)" />
                  <span class="badge"
                        [class.badge-success]="u.isActive"
                        [class.badge-danger]="!u.isActive">
                    {{ u.isActive ? 'Active' : 'Disabled' }}
                  </span>
                </label>
              </td>
              <td class="text-right">
                <button type="button" class="btn-sm btn-ghost text-danger"
                        (click)="remove(u)" [disabled]="u.isDeleted">Delete</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="6" class="empty">No users.</td></tr> }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .page-head {
      display:flex; align-items:flex-end; justify-content:space-between; gap:1rem;
      margin-bottom: 1rem; flex-wrap: wrap;
    }
    .page-head h2 { margin: 0; }
    .page-head .muted { margin: 0; }
    .filters { display:flex; gap:.5rem; min-width: 280px; }
    .filters input { width: 280px; }

    .deleted { opacity:.55; }
    .deleted td:first-child { text-decoration: line-through; }
    .inline-select { padding: .25rem .45rem; font-size: .85rem; }
    .toggle {
      display:inline-flex; align-items:center; gap:.5rem;
      cursor:pointer; user-select:none;
      font-size: .85rem; font-weight: 500; color: var(--color-text);
    }
    .toggle input { margin: 0; }
  `],
})
export class AdminUsersComponent implements OnInit {
  protected search = '';
  protected result = signal<PaginatedResult<AdminUser> | null>(null);
  protected err = signal<string | null>(null);

  private readonly repo = inject(AdminUsersRepository);

  ngOnInit(): void { this.reload(); }

  protected reload(): void {
    this.err.set(null);
    this.repo.list({
      search: this.search || undefined,
      pageSize: 50,
    }).subscribe({
      next: (r) => this.result.set(r),
      error: (e: unknown) => this.err.set(extractError(e).message),
    });
  }

  protected setRole(u: AdminUser, role: UserRole): void {
    this.repo.update(u.id, { role }).subscribe({
      next: () => this.reload(),
      error: (e: unknown) => { this.err.set(extractError(e).message); this.reload(); },
    });
  }

  protected setActive(u: AdminUser, ev: Event): void {
    const isActive = (ev.target as HTMLInputElement).checked;
    this.repo.update(u.id, { isActive }).subscribe({
      next: () => this.reload(),
      error: (e: unknown) => { this.err.set(extractError(e).message); this.reload(); },
    });
  }

  protected remove(u: AdminUser): void {
    if (!confirm(`Delete user ${u.fullName}? Their data will be removed.`)) return;
    this.repo.delete(u.id).subscribe({
      next: () => this.reload(),
      error: (e: unknown) => this.err.set(extractError(e).message),
    });
  }
}
