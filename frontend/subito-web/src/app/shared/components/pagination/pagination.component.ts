import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';
import { Component, ElementRef, EventEmitter, Input, Output, afterRender, inject } from '@angular/core';
import { I18nService } from '../../../core/services/i18n.service';

@Component({
  selector: 'app-pagination',
  standalone: true, imports: [TranslatePipe, IconComponent],
  template: `
    @if (totalPages > 1 || page > 1) {
      <nav class="pagination" [attr.aria-label]="labelKey | t" [attr.aria-busy]="disabled">
        <span class="muted" aria-live="polite" aria-atomic="true">{{ 'pagination.page' | t }} {{ page }} / {{ totalPages }} ({{ totalCount }} {{ 'pagination.records' | t }})</span>
        <div class="pagination-actions">
          <button class="btn btn-ghost" type="button" [disabled]="disabled || page <= 1" (click)="select(page - 1)">
            <app-icon class="directional" name="left" />{{ 'pagination.previous' | t }}
          </button>
          @for (number of pageNumbers; track $index) {
            @if (number !== null) {
              <button class="btn" [class.btn-primary]="number === page" [class.btn-ghost]="number !== page" type="button"
                [attr.aria-current]="number === page ? 'page' : null" [attr.aria-label]="pageLabel(number)"
                [disabled]="disabled" (click)="select(number)">{{ number }}</button>
            } @else { <span aria-hidden="true">…</span> }
          }
          <button
            class="btn btn-ghost"
            type="button"
            [disabled]="disabled || page >= totalPages"
            (click)="select(page + 1)"
          >
            {{ 'pagination.next' | t }}<app-icon class="directional" name="right" />
          </button>
        </div>
      </nav>
    }
  `,
  styles: [`.pagination{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:.75rem;margin-top:1rem}.pagination-actions{display:flex;align-items:center;flex-wrap:wrap;gap:.35rem}.pagination-actions .btn{min-width:2.5rem;min-height:2.75rem;padding:.45rem .65rem}`],
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Input() disabled = false;
  @Input() labelKey = 'pagination.navigation';
  private readonly i18n = inject(I18nService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private restoreFocus = false;
  private requestedPage = 1;
  private wasLoading = false;
  @Output() change = new EventEmitter<number>();
  constructor() {
    afterRender(() => {
      if (!this.restoreFocus) return;
      if (this.disabled) { this.wasLoading = true; return; }
      if (this.page !== this.requestedPage && !this.wasLoading) return;
      this.restoreFocus = false;
      if (document.activeElement !== document.body && !this.host.nativeElement.contains(document.activeElement)) return;
      const target = this.host.nativeElement.querySelector<HTMLElement>('[aria-current="page"]')
        ?? this.host.nativeElement.parentElement?.querySelector<HTMLElement>('h1');
      if (target) {
        const hadTabIndex = target.hasAttribute('tabindex');
        if (!hadTabIndex) target.setAttribute('tabindex', '-1');
        target.focus();
        if (!hadTabIndex) target.removeAttribute('tabindex');
      }
    });
  }
  get pageNumbers(): (number | null)[] {
    const last = Math.max(1, this.totalPages);
    const numbers = new Set([1, last]);
    for (let number = Math.max(1, this.page - 1); number <= Math.min(last, this.page + 1); number++) numbers.add(number);
    const result: (number | null)[] = [];
    let previous = 0;
    for (const number of [...numbers].sort((a, b) => a - b)) {
      if (number - previous > 1) result.push(null);
      result.push(number); previous = number;
    }
    return result;
  }
  pageLabel(number: number): string { return this.i18n.t('pagination.goTo').replace('{page}', String(number)); }
  select(number: number): void {
    if (!this.disabled && Number.isInteger(number) && number >= 1 && number <= this.totalPages && number !== this.page) {
      this.restoreFocus = this.host.nativeElement.contains(document.activeElement);
      this.requestedPage = number;
      this.wasLoading = false;
      this.change.emit(number);
    }
  }
}
