import { TooltipDirective } from '../../directives/tooltip.directive';
import { IconComponent } from '../icon/icon.component';
import { Component, Input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-search-field',
  standalone: true,
  imports: [TooltipDirective, IconComponent, ReactiveFormsModule, TranslatePipe],
  template: `
    <label class="search-field" [class.is-focused]="focused()" [class.has-value]="!!control.value">
      <span class="search-field__icon" aria-hidden="true">
        <app-icon name="search" />
      </span>
      <input
        type="search"
        [formControl]="control"
        [placeholder]="placeholderKey | t"
        [attr.aria-label]="placeholderKey | t"
        (focus)="focused.set(true)"
        (blur)="focused.set(false)"
        autocomplete="off"
        enterkeyhint="search"
      />
      @if (control.value) {
        <button
          type="button"
          class="search-field__clear"
          (click)="clear()"
          appTooltip [attr.aria-label]="'actions.clear' | t"
        >
          <app-icon name="close" />
        </button>
      }
    </label>
  `,
  styles: [
    `
      :host {
        display: block;
        width: min(100%, 360px);
        flex: 1 1 220px;
      }

      .search-field {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        width: 100%;
        min-height: 2.85rem;
        padding: 0.35rem 0.75rem;
        border: 1px solid var(--subito-border);
        border-radius: 999px;
        background:
          linear-gradient(180deg, #ffffff 0%, var(--subito-page) 100%);
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.85);
        transition: border-color 0.15s ease, box-shadow 0.15s ease, background 0.15s ease;
        cursor: text;
      }

      .search-field.is-focused {
        border-color: color-mix(in srgb, var(--subito-navy) 55%, var(--subito-border));
        box-shadow:
          0 0 0 3px rgba(0, 199, 177, 0.12),
          inset 0 1px 0 rgba(255, 255, 255, 0.9);
        background: #fff;
      }

      .search-field__icon {
        display: inline-flex;
        color: var(--subito-muted);
        flex-shrink: 0;
      }

      .search-field.is-focused .search-field__icon,
      .search-field.has-value .search-field__icon {
        color: var(--subito-navy);
      }

      input {
        flex: 1;
        min-width: 0;
        border: 0;
        outline: 0;
        background: transparent;
        color: var(--subito-navy);
        padding: 0.35rem 0;
        font: inherit;
      }

      input::placeholder {
        color: color-mix(in srgb, var(--subito-muted) 85%, transparent);
      }

      input::-webkit-search-cancel-button,
      input::-webkit-search-decoration {
        -webkit-appearance: none;
        appearance: none;
      }

      .search-field__clear {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 44px;
        height: 44px;
        border: 0;
        border-radius: 999px;
        background: var(--subito-teal-soft);
        color: var(--subito-navy);
        cursor: pointer;
        flex-shrink: 0;
        transition: background 0.15s ease, color 0.15s ease;
      }

      .search-field__clear:hover {
        background: color-mix(in srgb, var(--subito-navy) 14%, var(--subito-teal-soft));
        color: var(--subito-navy);
      }
    `,
  ],
})
export class SearchFieldComponent {
  @Input({ required: true }) control!: FormControl<string>;
  @Input() placeholderKey = 'actions.search';
  readonly focused = signal(false);

  clear(): void {
    this.control.setValue('');
    this.control.markAsDirty();
  }
}
