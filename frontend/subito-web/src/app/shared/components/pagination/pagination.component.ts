import { TranslatePipe } from '../../pipes/translate.pipe';
import { IconComponent } from '../icon/icon.component';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true, imports: [TranslatePipe, IconComponent],
  template: `
    @if (totalPages > 1) {
      <div class="row" style="justify-content: space-between; margin-top: 1rem">
        <span class="muted">{{ 'pagination.page' | t }} {{ page }} / {{ totalPages }} ({{ totalCount }})</span>
        <div class="row">
          <button class="btn btn-ghost" type="button" [disabled]="page <= 1" (click)="change.emit(page - 1)">
            <app-icon class="directional" name="left" />{{ 'pagination.previous' | t }}
          </button>
          <button
            class="btn btn-ghost"
            type="button"
            [disabled]="page >= totalPages"
            (click)="change.emit(page + 1)"
          >
            {{ 'pagination.next' | t }}<app-icon class="directional" name="right" />
          </button>
        </div>
      </div>
    }
  `,
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Output() change = new EventEmitter<number>();
}
