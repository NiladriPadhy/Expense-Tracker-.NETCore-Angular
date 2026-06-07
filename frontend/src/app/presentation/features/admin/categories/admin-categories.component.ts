import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Category, EntryType } from '../../../../domain/models';
import { AdminCategoriesRepository } from '../../../../domain/ports/admin.repository';
import { extractError } from '../../../shared/problem-details';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="page-head">
      <div>
        <h2>Manage categories</h2>
        <p class="muted">{{ categories().length }} categories</p>
      </div>
    </header>

    <form (ngSubmit)="create()" class="create card">
      <h3>Add category</h3>
      <div class="create-row">
        <label>Name <input placeholder="e.g. Groceries" [(ngModel)]="newName" name="n" required /></label>
        <label>Type
          <select [(ngModel)]="newType" name="t">
            <option value="Expense">Expense</option>
            <option value="Income">Income</option>
          </select>
        </label>
        <button type="submit" [disabled]="!newName">+ Add</button>
      </div>
    </form>

    @if (err()) { <div class="alert alert-danger">{{ err() }}</div> }

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr><th>Name</th><th>Type</th><th>Status</th></tr>
        </thead>
        <tbody>
          @for (c of categories(); track c.id) {
            <tr>
              <td><input class="inline-input" [(ngModel)]="c.name" (blur)="rename(c)" /></td>
              <td>
                <span class="badge"
                      [class.badge-success]="c.type === 'Income'"
                      [class.badge-danger]="c.type === 'Expense'">
                  {{ c.type }}
                </span>
              </td>
              <td>
                <label class="toggle">
                  <input type="checkbox" [checked]="c.isActive" (change)="setActive(c, $event)" />
                  <span class="badge"
                        [class.badge-success]="c.isActive"
                        [class.badge-danger]="!c.isActive">
                    {{ c.isActive ? 'Active' : 'Disabled' }}
                  </span>
                </label>
              </td>
            </tr>
          } @empty { <tr><td colspan="3" class="empty">No categories yet.</td></tr> }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .page-head { margin-bottom: 1rem; }
    .page-head h2 { margin: 0; }
    .page-head .muted { margin: 0; }

    .create { margin-bottom: 1.25rem; }
    .create h3 { margin: 0 0 .65rem; font-size: .95rem; }
    .create-row {
      display: grid;
      grid-template-columns: 2fr 1fr auto;
      gap: .75rem;
      align-items: end;
    }
    .create-row button { padding: .55rem 1rem; }

    .inline-input { padding: .3rem .5rem; max-width: 320px; }
    .toggle {
      display:inline-flex; align-items:center; gap:.5rem;
      cursor:pointer; user-select:none;
      font-size: .85rem;
    }
    .toggle input { margin: 0; }

    @media (max-width: 600px) {
      .create-row { grid-template-columns: 1fr; }
    }
  `],
})
export class AdminCategoriesComponent implements OnInit {
  protected categories = signal<Category[]>([]);
  protected newName = '';
  protected newType: EntryType = 'Expense';
  protected err = signal<string | null>(null);

  private readonly repo = inject(AdminCategoriesRepository);

  ngOnInit(): void { this.reload(); }

  private reload(): void {
    this.repo.list().subscribe({ next: (l) => this.categories.set(l) });
  }

  protected create(): void {
    if (!this.newName) return;
    this.repo.create({ name: this.newName, type: this.newType }).subscribe({
      next: () => { this.newName = ''; this.reload(); },
      error: (e) => this.err.set(extractError(e).message),
    });
  }

  protected rename(c: Category): void {
    this.repo.update(c.id, { name: c.name }).subscribe({ error: (e) => { this.err.set(extractError(e).message); this.reload(); } });
  }

  protected setActive(c: Category, ev: Event): void {
    const isActive = (ev.target as HTMLInputElement).checked;
    this.repo.update(c.id, { isActive }).subscribe({ next: () => this.reload(), error: (e) => { this.err.set(extractError(e).message); this.reload(); } });
  }
}
