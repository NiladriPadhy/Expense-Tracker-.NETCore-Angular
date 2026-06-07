import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import type { ChartConfiguration } from 'chart.js';
import { DashboardMonthPoint } from '../../../domain/models';

@Component({
  selector: 'app-expense-trend-chart',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  template: `
    @if (data().length > 0) {
      <div class="chart-host">
        <canvas baseChart
          [type]="'bar'"
          [data]="chartData()"
          [options]="chartOptions">
        </canvas>
      </div>
    } @else {
      <p class="muted">No history yet — record some entries.</p>
    }
  `,
  styles: [`
    .chart-host { position: relative; height: 280px; }
    .muted { text-align:center; color:#9ca3af; padding:1rem; }
  `],
})
export class ExpenseTrendChartComponent {
  readonly data = input.required<DashboardMonthPoint[]>();

  protected readonly chartData = computed<ChartConfiguration<'bar'>['data']>(() => {
    const points = this.data();
    return {
      labels: points.map((p) => `${p.year}-${p.month.toString().padStart(2, '0')}`),
      datasets: [
        {
          label: 'Income',
          data: points.map((p) => p.totalIncome),
          backgroundColor: 'rgba(16, 185, 129, 0.7)',
        },
        {
          label: 'Expense',
          data: points.map((p) => p.totalExpense),
          backgroundColor: 'rgba(239, 68, 68, 0.7)',
        },
        {
          label: 'Savings',
          data: points.map((p) => p.savings),
          type: 'line' as const,
          borderColor: '#2563eb',
          backgroundColor: 'rgba(37, 99, 235, 0.15)',
          tension: 0.25,
        } as unknown as ChartConfiguration<'bar'>['data']['datasets'][number],
      ],
    };
  });

  protected readonly chartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } },
    scales: {
      x: { stacked: false },
      y: { beginAtZero: true },
    },
  };
}
