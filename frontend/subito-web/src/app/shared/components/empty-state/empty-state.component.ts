import { IconComponent } from '../icon/icon.component';
import { Component, Input } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [IconComponent, TranslatePipe],
  template: `
    <div class="card" style="text-align: center; padding: 2rem">
      <app-icon name="search" /><p class="muted">{{ messageKey | t: fallback }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  @Input() messageKey = 'empty.generic';
  @Input() fallback = 'Nothing here yet';
}
