import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  template: `
    <div class="toast-host" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class]="toast.type" (click)="toastService.dismiss(toast.id)">
          {{ toast.text }}
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  readonly toastService = inject(ToastService);
}
