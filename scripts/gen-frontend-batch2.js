const fs = require('fs');
const path = require('path');
const root = path.join('frontend', 'subito-web', 'src', 'app', 'features');
const write = (rel, content) => {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  console.log(rel);
};

// ---- Client auth ----
write('client/auth/client-login.component.ts', `import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-login',
  standalone: true,
  imports: [FormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: \`
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.login' | t }}</h1>
        @if (!otpSent()) {
          <form (ngSubmit)="send()">
            <div class="field"><label>{{ 'auth.email' | t }}</label><input type="email" [(ngModel)]="email" name="email" required /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
          </form>
        } @else {
          <form (ngSubmit)="verify()">
            <p class="muted">Code sent to {{ email }}</p>
            @if (devOtp()) { <p class="badge">Dev OTP: {{ devOtp() }}</p> }
            <div class="field"><label>{{ 'auth.otp' | t }}</label><input [(ngModel)]="otp" name="otp" required /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
        }
        <p class="muted">No account? <a routerLink="/auth/register">{{ 'nav.register' | t }}</a></p>
      </div>
    </div>
  \`,
})
export class ClientLoginComponent {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  email = '';
  otp = '';
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);

  send(): void {
    this.busy.set(true);
    this.auth.clientLogin({ email: this.email }).subscribe({
      next: (res) => {
        this.otpSent.set(true);
        this.devOtp.set(res.developmentOtp ?? null);
        this.toast.success(res.message || 'OTP sent');
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }

  verify(): void {
    this.busy.set(true);
    this.auth.clientVerifyLogin({ email: this.email, otp: this.otp }).subscribe({
      next: () => { this.toast.success('Welcome'); void this.router.navigateByUrl('/'); },
      error: () => this.busy.set(false),
    });
  }
}
`);

write('client/auth/client-register.component.ts', `import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-register',
  standalone: true,
  imports: [FormsModule, RouterLink, LogoComponent, TranslatePipe],
  template: \`
    <div class="auth-shell">
      <div class="card auth-card stack">
        <app-logo />
        <h1 class="page-title">{{ 'nav.register' | t }}</h1>
        @if (!otpSent()) {
          <form (ngSubmit)="send()">
            <div class="field"><label>{{ 'auth.firstName' | t }}</label><input [(ngModel)]="firstName" name="firstName" required /></div>
            <div class="field"><label>{{ 'auth.lastName' | t }}</label><input [(ngModel)]="lastName" name="lastName" required /></div>
            <div class="field"><label>{{ 'auth.email' | t }}</label><input type="email" [(ngModel)]="email" name="email" required /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.sendOtp' | t }}</button>
          </form>
        } @else {
          <form (ngSubmit)="verify()">
            @if (devOtp()) { <p class="badge">Dev OTP: {{ devOtp() }}</p> }
            <div class="field"><label>{{ 'auth.otp' | t }}</label><input [(ngModel)]="otp" name="otp" required /></div>
            <button class="btn btn-primary" type="submit" [disabled]="busy()">{{ 'auth.verify' | t }}</button>
          </form>
        }
        <p class="muted"><a routerLink="/auth/login">{{ 'nav.login' | t }}</a></p>
      </div>
    </div>
  \`,
})
export class ClientRegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  firstName = ''; lastName = ''; email = ''; otp = '';
  readonly otpSent = signal(false);
  readonly devOtp = signal<string | null>(null);
  readonly busy = signal(false);

  send(): void {
    this.busy.set(true);
    this.auth.clientRegister({ firstName: this.firstName, lastName: this.lastName, email: this.email }).subscribe({
      next: (res) => { this.otpSent.set(true); this.devOtp.set(res.developmentOtp ?? null); this.toast.success(res.message || 'OTP sent'); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  verify(): void {
    this.busy.set(true);
    this.auth.clientVerifyRegistration({ email: this.email, otp: this.otp }).subscribe({
      next: () => { this.toast.success('Account created'); void this.router.navigateByUrl('/'); },
      error: () => this.busy.set(false),
    });
  }
}
`);

write('client/product/product-detail.component.ts', `import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClientProductDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { TokenStoreService } from '../../../core/services/token-store.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe],
  template: \`
    @if (product()) {
      <div class="card stack">
        @if (product()!.imageUrl) { <img class="thumb-lg" [src]="product()!.imageUrl!" [alt]="product()!.name" /> }
        <h1 class="page-title">{{ product()!.name }}</h1>
        <p>{{ product()!.description }}</p>
        <strong>{{ product()!.price | number:'1.2-2' }}</strong>
        <div class="row">
          <button class="btn btn-primary" type="button" (click)="add()">{{ 'actions.addToCart' | t }}</button>
          <a class="btn btn-ghost" [routerLink]="['/stores', product()!.providerId]">{{ 'actions.back' | t }}</a>
        </div>
      </div>
    }
  \`,
})
export class ProductDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly tokens = inject(TokenStoreService);
  private readonly toast = inject(ToastService);
  readonly product = signal<ClientProductDto | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('productId')!;
    this.api.get<ClientProductDto>(\`/client/products/\${id}\`).subscribe({ next: (p) => this.product.set(p) });
  }

  add(): void {
    const p = this.product();
    if (!p) return;
    if (!this.tokens.isAuthenticated('client')) { this.toast.info('Please sign in'); return; }
    this.api.post(\`/client/\${p.providerId}/cart/items\`, { productId: p.id, quantity: 1 }).subscribe({
      next: () => this.toast.success('Added to cart'),
    });
  }
}
`);

write('client/cart/cart.component.ts', `import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CreatePaymentSessionDto, ShoppingCartDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, EmptyStateComponent, TranslatePipe],
  template: \`
    <div class="toolbar">
      <h1 class="page-title">{{ 'nav.cart' | t }}</h1>
      <a class="btn btn-ghost" [routerLink]="['/stores', providerId]">{{ 'actions.back' | t }}</a>
    </div>
    @if (!cart() || !cart()!.items.length) { <app-empty-state messageKey="empty.cart" /> }
    @else {
      <div class="card stack">
        @for (item of cart()!.items; track item.id) {
          <div class="row" style="justify-content:space-between">
            <div class="row">
              @if (item.productImage) { <img class="thumb" [src]="item.productImage" [alt]="item.productName" /> }
              <div>
                <strong>{{ item.productName }}</strong>
                <div class="muted">{{ item.price | number:'1.2-2' }}</div>
              </div>
            </div>
            <div class="row">
              <input type="number" min="1" style="width:70px" [ngModel]="item.quantity" (ngModelChange)="update(item.id, $event)" />
              <button class="btn btn-ghost" type="button" (click)="remove(item.id)">{{ 'actions.delete' | t }}</button>
            </div>
          </div>
        }
        <div class="row" style="justify-content:space-between">
          <strong>Total: {{ cart()!.totalPrice | number:'1.2-2' }}</strong>
          <button class="btn btn-primary" type="button" [disabled]="busy()" (click)="checkout()">{{ 'actions.checkout' | t }}</button>
        </div>
      </div>
    }
  \`,
})
export class CartComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  providerId = '';
  readonly cart = signal<ShoppingCartDto | null>(null);
  readonly busy = signal(false);

  ngOnInit(): void {
    this.providerId = this.route.snapshot.paramMap.get('providerId') || '';
    this.reload();
  }

  reload(): void {
    this.api.get<ShoppingCartDto>(\`/client/\${this.providerId}/cart\`).subscribe({
      next: (c) => this.cart.set(c),
      error: () => this.cart.set(null),
    });
  }

  update(id: string, quantity: number): void {
    this.api.put(\`/client/\${this.providerId}/cart/items/\${id}\`, { quantity: Number(quantity) }).subscribe({ next: () => this.reload() });
  }

  remove(id: string): void {
    this.api.delete(\`/client/\${this.providerId}/cart/items/\${id}\`).subscribe({ next: () => this.reload() });
  }

  checkout(): void {
    this.busy.set(true);
    this.api.post<CreatePaymentSessionDto>(\`/client/\${this.providerId}/payments\`).subscribe({
      next: (session) => { window.location.href = session.url; },
      error: () => this.busy.set(false),
    });
  }
}
`);

write('client/orders/orders-list.component.ts', `import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS } from '../../../core/models/api.models';
import { OrderListItemDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-orders-list',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, EmptyStateComponent, PaginationComponent, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.orders' | t }}</h1>
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="table-wrap card">
        <table class="data">
          <thead><tr><th>Date</th><th>Total</th><th>Status</th><th></th></tr></thead>
          <tbody>
            @for (o of items(); track o.id) {
              <tr>
                <td>{{ o.orderDateUtc | date:'medium' }}</td>
                <td>{{ o.totalAmount | number:'1.2-2' }}</td>
                <td><span class="badge">{{ labels[o.status] || o.status }}</span></td>
                <td><a [routerLink]="['/orders', o.id]">View</a></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <app-pagination [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" (change)="load($event)" />
    }
  \`,
})
export class OrdersListComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly items = signal<OrderListItemDto[]>([]);
  readonly labels = ORDER_STATUS_LABELS;
  page = 1; totalPages = 1; totalCount = 0;
  ngOnInit(): void { this.load(1); }
  load(page: number): void {
    this.page = page;
    this.api.get<PagedResult<OrderListItemDto>>('/client/orders', { pageNumber: page, pageSize: 10 }).subscribe({
      next: (res) => { this.items.set(res.items ?? []); this.totalPages = res.totalPages; this.totalCount = res.totalCount; },
    });
  }
}
`);

write('client/orders/order-detail.component.ts', `import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ORDER_STATUS_LABELS, OrderStatus } from '../../../core/models/api.models';
import { OrderDetailDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, TranslatePipe],
  template: \`
    @if (order()) {
      <div class="toolbar">
        <h1 class="page-title">Order</h1>
        <a routerLink="/orders" class="btn btn-ghost">{{ 'actions.back' | t }}</a>
      </div>
      <div class="card stack">
        <div class="row" style="justify-content:space-between">
          <span>{{ order()!.orderDateUtc | date:'medium' }}</span>
          <span class="badge">{{ labels[order()!.status] }}</span>
        </div>
        @for (i of order()!.items; track i.id) {
          <div class="row" style="justify-content:space-between">
            <span>{{ i.productName }} × {{ i.quantity }}</span>
            <span>{{ i.lineTotal | number:'1.2-2' }}</span>
          </div>
        }
        <strong>Total: {{ order()!.totalAmount | number:'1.2-2' }}</strong>
        @if (order()!.status === pending) {
          <button class="btn btn-danger" type="button" (click)="cancel()">Cancel order</button>
        }
      </div>
    }
  \`,
})
export class OrderDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly order = signal<OrderDetailDto | null>(null);
  readonly labels = ORDER_STATUS_LABELS;
  readonly pending = OrderStatus.Pending;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('orderId')!;
    this.api.get<OrderDetailDto>(\`/client/orders/\${id}\`).subscribe({ next: (o) => this.order.set(o) });
  }

  cancel(): void {
    const id = this.order()?.id;
    if (!id) return;
    this.api.post(\`/client/orders/\${id}/cancel\`).subscribe({
      next: () => { this.toast.success('Cancelled'); this.ngOnInit(); },
    });
  }
}
`);

write('client/payment/payment-success.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderDetailDto } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: \`
    <div class="card stack">
      <h1 class="page-title">{{ 'payment.success' | t }}</h1>
      @if (!order()) { <p class="muted">{{ 'payment.waitingOrder' | t }}</p> }
      @else {
        <p>Order confirmed.</p>
        <a class="btn btn-primary" [routerLink]="['/orders', order()!.id]">View order</a>
      }
    </div>
  \`,
})
export class PaymentSuccessComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly order = signal<OrderDetailDto | null>(null);
  private attempts = 0;

  ngOnInit(): void {
    const sessionId = this.route.snapshot.queryParamMap.get('session_id');
    if (!sessionId) return;
    this.poll(sessionId);
  }

  private poll(sessionId: string): void {
    this.api.get<OrderDetailDto>(\`/client/orders/by-session/\${sessionId}\`).subscribe({
      next: (o) => this.order.set(o),
      error: () => {
        if (this.attempts++ < 8) setTimeout(() => this.poll(sessionId), 1500);
      },
    });
  }
}
`);

write('client/payment/payment-cancel.component.ts', `import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-payment-cancel',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: \`
    <div class="card stack">
      <h1 class="page-title">{{ 'payment.cancel' | t }}</h1>
      <p class="muted">Your cart is unchanged.</p>
      <a class="btn btn-primary" routerLink="/">{{ 'nav.home' | t }}</a>
    </div>
  \`,
})
export class PaymentCancelComponent {}
`);

write('client/profile/client-profile.component.ts', `import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfileDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-profile',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.profile' | t }}</h1>
    @if (profile()) {
      <form class="card stack" (ngSubmit)="save()">
        <div class="field"><label>{{ 'auth.firstName' | t }}</label><input [(ngModel)]="firstName" name="firstName" /></div>
        <div class="field"><label>{{ 'auth.lastName' | t }}</label><input [(ngModel)]="lastName" name="lastName" /></div>
        <div class="field"><label>{{ 'auth.email' | t }}</label><input [value]="profile()!.email" disabled /></div>
        <button class="btn btn-primary" type="submit">{{ 'actions.save' | t }}</button>
      </form>
      <form class="card stack" style="margin-top:1rem" (ngSubmit)="requestEmail()">
        <h3>Change email</h3>
        <div class="field"><label>New email</label><input [(ngModel)]="newEmail" name="newEmail" /></div>
        @if (otpSent()) {
          <div class="field"><label>{{ 'auth.otp' | t }}</label><input [(ngModel)]="otp" name="otp" /></div>
          <button class="btn btn-secondary" type="button" (click)="confirmEmail()">{{ 'auth.verify' | t }}</button>
        } @else {
          <button class="btn btn-ghost" type="submit">{{ 'auth.sendOtp' | t }}</button>
        }
      </form>
    }
  \`,
})
export class ClientProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly profile = signal<ProfileDto | null>(null);
  readonly otpSent = signal(false);
  firstName = ''; lastName = ''; newEmail = ''; otp = '';

  ngOnInit(): void {
    this.auth.getProfile('client').subscribe({
      next: (p) => { this.profile.set(p); this.firstName = p.firstName; this.lastName = p.lastName; },
    });
  }

  save(): void {
    this.auth.updateProfile('client', { firstName: this.firstName, lastName: this.lastName }).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success('Saved'); },
    });
  }

  requestEmail(): void {
    this.auth.requestEmailChange('client', { newEmail: this.newEmail }).subscribe({
      next: (r) => { this.otpSent.set(true); this.toast.success(r.message || 'OTP sent'); },
    });
  }

  confirmEmail(): void {
    this.auth.confirmEmailChange('client', { newEmail: this.newEmail, otp: this.otp }).subscribe({
      next: (p) => { this.profile.set(p); this.toast.success('Email updated'); this.otpSent.set(false); },
    });
  }
}
`);

write('client/notifications/client-notifications.component.ts', `import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NotificationDto, PagedResult } from '../../../core/models/domain.models';
import { ApiService } from '../../../core/services/api.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-client-notifications',
  standalone: true,
  imports: [DatePipe, EmptyStateComponent, TranslatePipe],
  template: \`
    <h1 class="page-title">{{ 'nav.notifications' | t }}</h1>
    @if (!items().length) { <app-empty-state /> } @else {
      <div class="stack">
        @for (n of items(); track n.id) {
          <article class="card" [style.opacity]="n.isRead ? 0.7 : 1" (click)="mark(n)">
            <strong>{{ n.title }}</strong>
            <p>{{ n.body }}</p>
            <span class="muted">{{ n.createdAtUtc | date:'medium' }}</span>
          </article>
        }
      </div>
    }
  \`,
})
export class ClientNotificationsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly items = signal<NotificationDto[]>([]);
  ngOnInit(): void {
    this.api.get<PagedResult<NotificationDto> | NotificationDto[]>('/client/notifications').subscribe({
      next: (res) => this.items.set(Array.isArray(res) ? res : res.items ?? []),
    });
  }
  mark(n: NotificationDto): void {
    if (n.isRead) return;
    this.api.post(\`/client/notifications/\${n.id}/read\`).subscribe({
      next: () => this.items.update((list) => list.map((x) => (x.id === n.id ? { ...x, isRead: true } : x))),
    });
  }
}
`);

console.log('client features done');
