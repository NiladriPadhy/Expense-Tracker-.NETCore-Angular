import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dashboard, StatusColor } from '../../../domain/models';
import { DashboardRepository } from '../../../domain/ports/dashboard.repository';
import { STATUS_COLOR_HEX } from '../../shared/savings-rate.classifier';
import { ExpenseTrendChartComponent } from './expense-trend-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, ExpenseTrendChartComponent],
  template: `
    <header class="page-head">
      <div>
        <h2>Dashboard</h2>
        <p class="muted">Your money at a glance.</p>
      </div>
      <label class="range">
        <span>Months back</span>
        <input type="number" [(ngModel)]="monthsBack" min="1" max="36" (change)="reload()" />
      </label>
    </header>

    @if (data(); as d) {
      @if (d.alertExpenseExceedsIncome) {
        <div class="alert alert-warning">
          <span>⚠</span>
          <span>Expenses are at or above income for the current month.</span>
        </div>
      }

      <section class="kpis">
        <article class="kpi card" [class.kpi--ok]="d.currentMonthStatusColor === 'Green'"
                 [class.kpi--warn]="d.currentMonthStatusColor === 'Orange'"
                 [class.kpi--bad]="d.currentMonthStatusColor === 'OrangeRedTint' || d.currentMonthStatusColor === 'BloodRed'">
          <div class="kpi__label">Current month savings rate</div>
          <div class="kpi__value" [style.color]="hex(d.currentMonthStatusColor)">
            {{ d.currentMonthSavingsRatePct | number:'1.1-1' }}%
          </div>
          <div class="kpi__sub">
            <span class="badge"
                  [class.badge-success]="d.currentMonthStatusColor === 'Green'"
                  [class.badge-warning]="d.currentMonthStatusColor === 'Orange'"
                  [class.badge-danger]="d.currentMonthStatusColor === 'OrangeRedTint' || d.currentMonthStatusColor === 'BloodRed'">
              {{ statusLabel(d.currentMonthStatusColor) }}
            </span>
          </div>
        </article>
        <article class="kpi card">
          <div class="kpi__label">Currency</div>
          <div class="kpi__value">{{ d.currencyCode }}</div>
          <div class="kpi__sub muted">Preferred display currency</div>
        </article>
        <article class="kpi card">
          <div class="kpi__label">History tracked</div>
          <div class="kpi__value">{{ d.trend.length }}</div>
          <div class="kpi__sub muted">months</div>
        </article>
      </section>

      <section class="card chart-card">
        <header class="card-head">
          <h3>Trend</h3>
          <span class="muted">Last {{ d.trend.length }} months</span>
        </header>
        <app-expense-trend-chart [data]="d.trend" />
      </section>

      <section class="card history-card">
        <header class="card-head">
          <h3>Monthly history</h3>
          <button type="button" class="btn-ghost btn-sm" (click)="showHistory.set(!showHistory())">
            {{ showHistory() ? 'Hide table' : 'Show table' }}
          </button>
        </header>
        @if (showHistory()) {
          <div class="table-wrap" style="border-radius:0; box-shadow:none; border:0; border-top:1px solid var(--color-border);">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Month</th>
                  <th class="numeric">Income</th>
                  <th class="numeric">Expense</th>
                  <th class="numeric">Savings</th>
                  <th class="numeric">Rate %</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (m of d.trend; track $index) {
                  <tr>
                    <td>{{ m.year }}-{{ pad(m.month) }}</td>
                    <td class="numeric text-success">{{ m.totalIncome | number:'1.2-2' }}</td>
                    <td class="numeric text-danger">{{ m.totalExpense | number:'1.2-2' }}</td>
                    <td class="numeric">{{ m.savings | number:'1.2-2' }}</td>
                    <td class="numeric">{{ m.savingsRatePct | number:'1.1-1' }}%</td>
                    <td>
                      <span class="badge"
                            [class.badge-success]="m.statusColor === 'Green'"
                            [class.badge-warning]="m.statusColor === 'Orange'"
                            [class.badge-danger]="m.statusColor === 'OrangeRedTint' || m.statusColor === 'BloodRed'">
                        {{ statusLabel(m.statusColor) }}
                      </span>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="6" class="empty">No history yet — record some entries.</td></tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    } @else if (loading()) {
      <div class="card text-center muted">Loading…</div>
    }
  `,
  styles: [`
    :host { display: block; }
    .page-head {
      display:flex; align-items:flex-end; justify-content:space-between; gap:1rem;
      margin-bottom: 1.25rem; flex-wrap: wrap;
    }
    .page-head h2 { margin: 0; }
    .page-head .muted { margin: 0; }
    .range { width: auto; max-width: 220px; }
    .range span { font-size: .8rem; }
    .range input { width: 120px; }

    .kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .kpi { padding: 1.1rem 1.25rem; }
    .kpi__label { color: var(--color-text-muted); font-size:.78rem; font-weight:600; letter-spacing:.04em; text-transform:uppercase; margin-bottom:.4rem; }
    .kpi__value { font-size: 1.85rem; font-weight: 700; line-height: 1.1; font-variant-numeric: tabular-nums; }
    .kpi__sub { margin-top: .4rem; font-size: .82rem; }
    .kpi--ok { border-left: 3px solid var(--color-success); }
    .kpi--warn { border-left: 3px solid var(--color-warning); }
    .kpi--bad { border-left: 3px solid var(--color-danger); }

    .chart-card, .history-card { padding: 0; margin-bottom: 1.25rem; }
    .chart-card { padding: 1.25rem; }
    .card-head { display:flex; align-items:center; justify-content:space-between; margin-bottom: .75rem; }
    .card-head h3 { margin: 0; }
    .history-card .card-head { padding: 1rem 1.25rem; margin: 0; }
  `],
})
export class DashboardComponent implements OnInit {
  protected monthsBack = 6;
  protected data = signal<Dashboard | null>(null);
  protected loading = signal(false);
  protected showHistory = signal(false);

  private readonly repo = inject(DashboardRepository);

  ngOnInit(): void { this.reload(); }

  protected hex(c: StatusColor): string { return STATUS_COLOR_HEX[c]; }
  protected pad(n: number): string { return n.toString().padStart(2, '0'); }

  protected statusLabel(c: StatusColor): string {
    switch (c) {
      case 'Green': return 'Healthy';
      case 'Orange': return 'Watch';
      case 'OrangeRedTint': return 'At risk';
      case 'BloodRed': return 'Critical';
      default: return c;
    }
  }

  protected reload(): void {
    this.loading.set(true);
    this.repo.get(this.monthsBack).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.data.set(null); this.loading.set(false); },
    });
  }
}
