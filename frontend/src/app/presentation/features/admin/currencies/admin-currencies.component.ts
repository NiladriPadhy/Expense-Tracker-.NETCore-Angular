import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Currency } from '../../../../domain/models';
import { AdminCurrenciesRepository } from '../../../../domain/ports/admin.repository';
import { extractError } from '../../../shared/problem-details';

@Component({
  selector: 'app-admin-currencies',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="page-head">
      <div>
        <h2>Manage currencies</h2>
        <p class="muted">{{ items().length }} currencies</p>
      </div>
    </header>

    <form (ngSubmit)="create()" class="create card">
      <h3>Add currency</h3>
      <div class="create-row">
        <label>Code <input placeholder="USD" [(ngModel)]="newCode" name="c" maxlength="3" required /></label>
        <label>Name <input placeholder="US Dollar" [(ngModel)]="newName" name="n" required /></label>
        <label>Symbol <input placeholder="$" [(ngModel)]="newSymbol" name="s" required /></label>
        <button type="submit">+ Add</button>
      </div>
    </form>

    @if (err()) { <div class="alert alert-danger">{{ err() }}</div> }

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr><th>Code</th><th>Name</th><th>Symbol</th><th>Status</th></tr>
        </thead>
        <tbody>
          @for (c of items(); track c.code) {
            <tr>
              <td><span class="badge badge-primary">{{ c.code }}</span></td>
              <td><input class="inline-input" [(ngModel)]="c.name" (blur)="save(c)" /></td>
              <td><input class="inline-input inline-input--sm" [(ngModel)]="c.symbol" (blur)="save(c)" /></td>
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
          } @empty { <tr><td colspan="4" class="empty">No currencies yet.</td></tr> }
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
      grid-template-columns: 100px 2fr 100px auto;
      gap: .75rem;
      align-items: end;
    }
    .create-row button { padding: .55rem 1rem; }

    .inline-input { padding: .3rem .5rem; max-width: 320px; }
    .inline-input--sm { max-width: 80px; }
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
export class AdminCurrenciesComponent implements OnInit {
  protected items = signal<Currency[]>([]);
  protected newCode = '';
  protected newName = '';
  protected newSymbol = '';
  protected err = signal<string | null>(null);

  private readonly repo = inject(AdminCurrenciesRepository);

  ngOnInit(): void { this.reload(); }

  private reload(): void { this.repo.list().subscribe({ next: (l) => this.items.set(l) }); }

  protected create(): void {
    this.repo.create({ code: this.newCode.toUpperCase(), name: this.newName, symbol: this.newSymbol }).subscribe({
      next: () => { this.newCode = ''; this.newName = ''; this.newSymbol = ''; this.reload(); },
      error: (e) => this.err.set(extractError(e).message),
    });
  }

  protected save(c: Currency): void {
    this.repo.update(c.code, { name: c.name, symbol: c.symbol }).subscribe({
      error: (e) => { this.err.set(extractError(e).message); this.reload(); },
    });
  }

  protected setActive(c: Currency, ev: Event): void {
    const isActive = (ev.target as HTMLInputElement).checked;
    this.repo.update(c.code, { isActive }).subscribe({
      next: () => this.reload(),
      error: (e) => { this.err.set(extractError(e).message); this.reload(); },
    });
  }
}
