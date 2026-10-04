import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/guards/auth.guards';
import { StorefrontLayoutComponent } from './layouts/storefront-layout/storefront-layout.component';

export const routes: Routes = [
  {
    path: 'auth/login',
    canActivate: [guestGuard('client', '/')],
    loadComponent: () =>
      import('./features/client/auth/client-login.component').then((m) => m.ClientLoginComponent),
  },
  {
    path: 'auth/register',
    canActivate: [guestGuard('client', '/')],
    loadComponent: () =>
      import('./features/client/auth/client-register.component').then((m) => m.ClientRegisterComponent),
  },
  {
    path: 'provider/login',
    canActivate: [guestGuard('provider', '/provider')],
    loadComponent: () =>
      import('./features/provider/auth/provider-login.component').then((m) => m.ProviderLoginComponent),
  },
  {
    path: 'provider/forgot-password',
    loadComponent: () =>
      import('./features/provider/auth/provider-forgot.component').then((m) => m.ProviderForgotComponent),
  },
  {
    path: 'admin/login',
    canActivate: [guestGuard('admin', '/admin')],
    loadComponent: () =>
      import('./features/admin/auth/admin-login.component').then((m) => m.AdminLoginComponent),
  },
  {
    path: 'admin/forgot-password',
    loadComponent: () =>
      import('./features/admin/auth/admin-forgot.component').then((m) => m.AdminForgotComponent),
  },
  {
    path: 'provider',
    canActivate: [authGuard('provider')],
    loadComponent: () =>
      import('./features/provider/dashboard/provider-shell.component').then((m) => m.ProviderShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/provider/dashboard/provider-home.component').then((m) => m.ProviderHomeComponent),
      },
      {
        path: 'store',
        canActivate: [permissionGuard('provider', ['ProviderStore.Read'])],
        loadComponent: () =>
          import('./features/provider/store/provider-store.component').then((m) => m.ProviderStoreComponent),
      },
      {
        path: 'categories',
        canActivate: [permissionGuard('provider', ['ProviderCategory.Read'])],
        loadComponent: () =>
          import('./features/provider/categories/provider-categories.component').then(
            (m) => m.ProviderCategoriesComponent
          ),
      },
      {
        path: 'products',
        canActivate: [permissionGuard('provider', ['ProviderProduct.Read'])],
        loadComponent: () =>
          import('./features/provider/products/provider-products.component').then(
            (m) => m.ProviderProductsComponent
          ),
      },
      {
        path: 'orders',
        canActivate: [permissionGuard('provider', ['ProviderOrder.Read'])],
        loadComponent: () =>
          import('./features/provider/orders/provider-orders.component').then((m) => m.ProviderOrdersComponent),
      },
      {
        path: 'orders/:orderId',
        canActivate: [permissionGuard('provider', ['ProviderOrder.Read'])],
        loadComponent: () =>
          import('./features/provider/orders/provider-orders.component').then((m) => m.ProviderOrdersComponent),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('provider', ['ProviderRoles.Read'])],
        loadComponent: () =>
          import('./features/provider/roles/provider-roles.component').then((m) => m.ProviderRolesComponent),
      },
      {
        path: 'staff',
        canActivate: [permissionGuard('provider', ['ProviderStaff.Read'])],
        loadComponent: () =>
          import('./features/provider/staff/provider-staff.component').then((m) => m.ProviderStaffComponent),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/provider/notifications/provider-notifications.component').then(
            (m) => m.ProviderNotificationsComponent
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/provider/profile/provider-profile.component').then((m) => m.ProviderProfileComponent),
      },
    ],
  },
  {
    path: 'admin',
    canActivate: [authGuard('admin')],
    loadComponent: () =>
      import('./features/admin/dashboard/admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/admin/dashboard/admin-home.component').then((m) => m.AdminHomeComponent),
      },
      {
        path: 'providers',
        canActivate: [permissionGuard('admin', ['Providers.Read'])],
        loadComponent: () =>
          import('./features/admin/providers/admin-providers.component').then((m) => m.AdminProvidersComponent),
      },
      {
        path: 'providers/:id',
        canActivate: [permissionGuard('admin', ['Providers.Read'])],
        loadComponent: () =>
          import('./features/admin/providers/admin-provider-detail.component').then(
            (m) => m.AdminProviderDetailComponent
          ),
      },
      {
        path: 'services',
        canActivate: [permissionGuard('admin', ['Services.Read'])],
        loadComponent: () =>
          import('./features/admin/services/admin-services.component').then((m) => m.AdminServicesComponent),
      },
      {
        path: 'clients',
        canActivate: [permissionGuard('admin', ['Clients.Read'])],
        loadComponent: () =>
          import('./features/admin/clients/admin-clients.component').then((m) => m.AdminClientsComponent),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('admin', ['Roles.Read'])],
        loadComponent: () =>
          import('./features/admin/roles/admin-roles.component').then((m) => m.AdminRolesComponent),
      },
      {
        path: 'users',
        canActivate: [permissionGuard('admin', ['Admins.Read'])],
        loadComponent: () =>
          import('./features/admin/users/admin-users.component').then((m) => m.AdminUsersComponent),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/admin/notifications/admin-notifications.component').then(
            (m) => m.AdminNotificationsComponent
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/admin/profile/admin-profile.component').then((m) => m.AdminProfileComponent),
      },
    ],
  },
  {
    path: '',
    component: StorefrontLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/client/home/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'services/:serviceId/providers',
        loadComponent: () =>
          import('./features/client/providers/providers-list.component').then((m) => m.ProvidersListComponent),
      },
      {
        path: 'stores/:providerId',
        loadComponent: () =>
          import('./features/client/store/store.component').then((m) => m.StoreComponent),
      },
      {
        path: 'products/:productId',
        loadComponent: () =>
          import('./features/client/product/product-detail.component').then((m) => m.ProductDetailComponent),
      },
      {
        path: 'cart',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/cart/carts-list.component').then((m) => m.CartsListComponent),
      },
      {
        path: 'cart/:providerId',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/cart/cart.component').then((m) => m.CartComponent),
      },
      {
        path: 'orders',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/orders/orders-list.component').then((m) => m.OrdersListComponent),
      },
      {
        path: 'orders/:orderId',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/orders/order-detail.component').then((m) => m.OrderDetailComponent),
      },
      {
        path: 'payment/success',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/payment/payment-success.component').then((m) => m.PaymentSuccessComponent),
      },
      {
        path: 'payment/cancel',
        loadComponent: () =>
          import('./features/client/payment/payment-cancel.component').then((m) => m.PaymentCancelComponent),
      },
      {
        path: 'profile',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/profile/client-profile.component').then((m) => m.ClientProfileComponent),
      },
      {
        path: 'notifications',
        canActivate: [authGuard('client')],
        loadComponent: () =>
          import('./features/client/notifications/client-notifications.component').then(
            (m) => m.ClientNotificationsComponent
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
