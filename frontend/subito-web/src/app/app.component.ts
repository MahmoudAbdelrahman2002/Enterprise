import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfirmHostComponent } from './shared/components/confirm-host/confirm-host.component';
import { ToastHostComponent } from './shared/components/toast/toast-host.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastHostComponent, ConfirmHostComponent],
  template: `
    <app-toast-host />
    <app-confirm-host />
    <router-outlet />
  `,
})
export class AppComponent {}
