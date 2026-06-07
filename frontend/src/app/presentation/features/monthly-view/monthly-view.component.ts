import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Category, Entry, EntryType, MonthlyView, StatusColor } from '../../../domain/models';
import { CategoriesRepository } from '../../../domain/ports/categories.repository';
import { EntriesRepository } from '../../../domain/ports/entries.repository';
import { MonthsRepository } from '../../../domain/ports/months.repository';
import { currentMonth } from '../../../domain/value-objects/month-year';
import { STATUS_COLOR_HEX } from '../../shared/savings-rate.classifier';
import { extractError } from '../../shared/problem-details';

interface EditingEntry {
  id?: string;
  type: EntryType;
  categoryId: string | null;
  freeText: string;
  amount: number;
  date: string;
  note: string;
}

@Component({
  selector: 'app-monthly-view',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe],
  template: `
    <header class="page-head">
      <div>
        <h2>Monthly view</h2>
        <p class="muted">Track income and expenses for a specific month.</p>
      </div>
      <div class="month-nav">
        <button type="button" class="btn-ghost" (click)="prevMonth()" aria-label="Previous month">‹</button>
        <strong class="month-label">{{ monthName() }} {{ year() }}</strong>
        <button type="button" class="btn-ghost" (click)="nextMonth()" aria-label="Next month">›</button>
        @if (view()?.readOnly) { <span class="badge badge-warning">Read-only</span> }
      </div>
    </header>

    @if (view(); as v) {
      <section class="kpis">
        <article class="kpi card">
          <div class="kpi__label">Opening</div>
          <div class="kpi__value numeric">{{ v.openingBalance | number:'1.2-2' }}</div>
          <div class="kpi__sub muted">{{ v.currencyCode }}</div>
        </article>
        <article class="kpi card kpi--income">
          <div class="kpi__label">Income</div>
          <div class="kpi__value numeric text-success">{{ v.totalIncome | number:'1.2-2' }}</div>
          <div class="kpi__sub muted">{{ v.currencyCode }}</div>
        </article>
        <article class="kpi card kpi--expense">
          <div class="kpi__label">Expense</div>
          <div class="kpi__value numeric text-danger">{{ v.totalExpense | number:'1.2-2' }}</div>
          <div class="kpi__sub muted">{{ v.currencyCode }}</div>
        </article>
        <article class="kpi card">
          <div class="kpi__label">Closing</div>
          <div class="kpi__value numeric">{{ v.closingBalance | number:'1.2-2' }}</div>
          <div class="kpi__sub muted">{{ v.currencyCode }}</div>
        </article>
        <article class="kpi card"
                 [class.kpi--ok]="v.statusColor === 'Green'"
                 [class.kpi--warn]="v.statusColor === 'Orange'"
                 [class.kpi--bad]="v.statusColor === 'OrangeRedTint' || v.statusColor === 'BloodRed'">
          <div class="kpi__label">Savings rate</div>
          <div class="kpi__value numeric" [style.color]="statusHex(v.statusColor)">
            {{ v.savingsRatePct | number:'1.1-1' }}%
          </div>
          <div class="kpi__sub">
            <span class="badge"
                  [class.badge-success]="v.statusColor === 'Green'"
                  [class.badge-warning]="v.statusColor === 'Orange'"
                  [class.badge-danger]="v.statusColor === 'OrangeRedTint' || v.statusColor === 'BloodRed'">
              {{ statusLabel(v.statusColor) }}
            </span>
          </div>
        </article>
      </section>
    }

    <section class="actions row-flex">
      <button type="button" class="btn-danger" (click)="newEntry('Expense')" [disabled]="readOnly()">+ Expense</button>
      <button type="button" class="btn-success" (click)="newEntry('Income')" [disabled]="readOnly()">+ Income</button>
    </section>

    @if (editing(); as e) {
      <form class="entry-form card" (ngSubmit)="save()" #f="ngForm">
        <h3>{{ e.id ? 'Edit' : 'New' }} {{ e.type }}</h3>
        <div class="form-grid">
          <label>Category
            <select name="catId" [(ngModel)]="e.categoryId" (ngModelChange)="onCategoryChange()">
              <option [ngValue]="null">— free text —</option>
              @for (c of categoriesForType(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
            </select>
          </label>
          @if (!e.categoryId) {
            <label>Free-text category <input name="ft" [(ngModel)]="e.freeText" required maxlength="100" /></label>
          }
          <label>Amount <input name="amount" type="number" step="0.01" min="0.01" [(ngModel)]="e.amount" required /></label>
          <label>Date <input name="date" type="date" [(ngModel)]="e.date" required /></label>
          <label class="span-2">Note <input name="note" [(ngModel)]="e.note" maxlength="500" placeholder="Optional" /></label>
        </div>
        @if (formError()) { <p class="alert alert-danger">{{ formError() }}</p> }
        <div class="row-flex">
          <button type="submit" [disabled]="!f.valid || busy()">
            {{ busy() ? 'Saving…' : 'Save' }}
          </button>
          <button type="button" class="btn-ghost" (click)="cancel()">Cancel</button>
        </div>
      </form>
    }

    <section class="entries-card">
      <header class="card-head">
        <h3>Entries</h3>
        <span class="muted">{{ (view()?.entries ?? []).length }} total</span>
      </header>
      <div class="table-wrap" style="border-radius:0; box-shadow:none; border:0; border-top:1px solid var(--color-border);">
        <table class="data-table">
          <thead>
            <tr>
              <th>Date</th><th>Type</th><th>Category</th>
              <th class="numeric">Amount</th><th>Note</th><th></th>
            </tr>
          </thead>
          <tbody>
            @for (e of view()?.entries ?? []; track e.id) {
              <tr>
                <td>{{ e.entryDate }}</td>
                <td>
                  <span class="badge"
                        [class.badge-success]="e.type === 'Income'"
                        [class.badge-danger]="e.type === 'Expense'">
                    {{ e.type }}
                  </span>
                </td>
                <td>{{ e.categoryName }}</td>
                <td class="numeric"
                    [class.text-success]="e.type === 'Income'"
                    [class.text-danger]="e.type === 'Expense'">
                  {{ e.amount | number:'1.2-2' }}
                </td>
                <td class="muted">{{ e.note }}</td>
                <td class="row-actions">
                  <button type="button" class="btn-sm btn-ghost" (click)="edit(e)" [disabled]="readOnly()">Edit</button>
                  <button type="button" class="btn-sm btn-ghost text-danger" (click)="remove(e)" [disabled]="readOnly()">Delete</button>
                </td>
              </tr>
            } @empty { <tr><td colspan="6" class="empty">No entries this month.</td></tr> }
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .page-head {
      display:flex; align-items:flex-end; justify-content:space-between; gap:1rem;
      margin-bottom: 1.25rem; flex-wrap: wrap;
    }
    .page-head h2 { margin: 0; }
    .page-head .muted { margin: 0; }

    .month-nav {
      display:inline-flex; align-items:center; gap:.5rem;
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-pill);
      padding: .25rem .5rem;
      box-shadow: var(--shadow-xs);
    }
    .month-nav button { border: 0; background: transparent; padding: .15rem .55rem; font-size: 1.15rem; line-height: 1; border-radius: var(--radius-pill); }
    .month-nav button:hover:not(:disabled) { background: var(--color-surface-2); }
    .month-label { min-width: 140px; text-align: center; font-weight: 600; }

    .kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .kpi { padding: 1rem 1.1rem; }
    .kpi__label { color: var(--color-text-muted); font-size:.72rem; font-weight:600; letter-spacing:.04em; text-transform:uppercase; margin-bottom:.4rem; }
    .kpi__value { font-size: 1.4rem; font-weight: 700; line-height: 1.15; }
    .kpi__sub { margin-top: .35rem; font-size: .78rem; }
    .kpi--income { border-top: 3px solid var(--color-success); }
    .kpi--expense { border-top: 3px solid var(--color-danger); }
    .kpi--ok { border-left: 3px solid var(--color-success); }
    .kpi--warn { border-left: 3px solid var(--color-warning); }
    .kpi--bad { border-left: 3px solid var(--color-danger); }

    .actions { margin-bottom: 1rem; }

    .entry-form { margin-bottom: 1.25rem; max-width: 720px; }
    .entry-form h3 { margin: 0 0 .75rem; }
    .form-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: .75rem;
      margin-bottom: .75rem;
    }
    .form-grid .span-2 { grid-column: 1 / -1; }
    @media (max-width: 540px) { .form-grid { grid-template-columns: 1fr; } }

    .entries-card {
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-sm);
      overflow: hidden;
      margin-top: .25rem;
    }
    .card-head {
      display:flex; align-items:center; justify-content:space-between;
      padding: 1rem 1.25rem;
    }
    .card-head h3 { margin: 0; }
    .row-actions { display:flex; gap:.25rem; justify-content: flex-end; }
  `],
})
export class MonthlyViewComponent implements OnInit {
  readonly y = input<string | undefined>();
  readonly m = input<string | undefined>();

  protected year = signal<number>(currentMonth().year);
  protected month = signal<number>(currentMonth().month);
  protected view = signal<MonthlyView | null>(null);
  protected categories = signal<Category[]>([]);
  protected editing = signal<EditingEntry | null>(null);
  protected formError = signal<string | null>(null);
  protected busy = signal(false);

  protected categoriesForType = computed(() => {
    const e = this.editing();
    return e ? this.categories().filter((c) => c.type === e.type) : [];
  });

  protected readOnly = computed(() => this.view()?.readOnly ?? false);

  private readonly monthsRepo = inject(MonthsRepository);
  private readonly entriesRepo = inject(EntriesRepository);
  private readonly categoriesRepo = inject(CategoriesRepository);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const ys = this.y(); const ms = this.m();
    if (ys && ms) { this.year.set(+ys); this.month.set(+ms); }
    this.load();
  }

  protected statusHex(c: StatusColor): string { return STATUS_COLOR_HEX[c]; }

  protected statusLabel(c: StatusColor): string {
    switch (c) {
      case 'Green': return 'Healthy';
      case 'Orange': return 'Watch';
      case 'OrangeRedTint': return 'At risk';
      case 'BloodRed': return 'Critical';
      default: return c;
    }
  }

  protected monthName(): string {
    const names = ['January','February','March','April','May','June','July','August','September','October','November','December'];
    return names[this.month() - 1] ?? '';
  }

  private load(): void {
    forkJoin({
      view: this.monthsRepo.getMonth(this.year(), this.month()),
      cats: this.categoriesRepo.listActive(),
    }).subscribe({
      next: ({ view, cats }) => { this.view.set(view); this.categories.set(cats); },
      error: () => this.view.set(null),
    });
  }

  protected prevMonth(): void {
    let y = this.year(); let m = this.month() - 1;
    if (m < 1) { m = 12; y--; }
    this.navigateTo(y, m);
  }

  protected nextMonth(): void {
    let y = this.year(); let m = this.month() + 1;
    if (m > 12) { m = 1; y++; }
    this.navigateTo(y, m);
  }

  private navigateTo(y: number, m: number): void {
    this.year.set(y); this.month.set(m);
    this.router.navigate(['/months', y, m]);
    this.load();
  }

  protected newEntry(type: EntryType): void {
    this.editing.set({
      type, categoryId: null, freeText: '', amount: 0,
      date: this.todayInMonth(), note: '',
    });
    this.formError.set(null);
  }

  protected onCategoryChange(): void {
    const e = this.editing(); if (!e) return;
    if (e.categoryId) e.freeText = '';
    this.editing.set({ ...e });
  }

  protected edit(e: Entry): void {
    const isFreeText = e.categoryId == null;
    this.editing.set({
      id: e.id, type: e.type, categoryId: e.categoryId, freeText: isFreeText ? e.categoryName : '',
      amount: e.amount, date: e.entryDate, note: e.note ?? '',
    });
    this.formError.set(null);
  }

  protected save(): void {
    const e = this.editing(); if (!e) return;
    this.busy.set(true); this.formError.set(null);
    const body = {
      entryDate: e.date,
      type: e.type,
      amount: e.amount,
      categoryId: e.categoryId ?? null,
      categoryFreeText: e.categoryId ? null : (e.freeText || null),
      note: e.note || null,
    };
    const obs = e.id ? this.entriesRepo.update(e.id, body) : this.entriesRepo.create(body);
    obs.subscribe({
      next: () => { this.editing.set(null); this.busy.set(false); this.load(); },
      error: (err: unknown) => { this.formError.set(extractError(err).message); this.busy.set(false); },
    });
  }

  protected remove(e: Entry): void {
    if (!confirm('Delete this entry?')) return;
    this.entriesRepo.delete(e.id).subscribe({ next: () => this.load() });
  }

  protected cancel(): void { this.editing.set(null); }

  private todayInMonth(): string {
    const y = this.year(); const m = this.month();
    const now = new Date();
    if (now.getUTCFullYear() === y && now.getUTCMonth() + 1 === m) {
      return now.toISOString().slice(0, 10);
    }
    return `${y}-${m.toString().padStart(2, '0')}-01`;
  }
}
