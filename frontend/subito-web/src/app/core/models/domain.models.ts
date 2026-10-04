import {
  AuthResponseDto,
  LocalizedText,
  OrderStatus,
  PagedResult,
  ProfileDto,
} from './api.models';

export interface ClientMarketServiceDto {
  id: string;
  name?: string | null;
  description?: string | null;
  imageUrl?: string | null;
}

export interface ClientProviderListItemDto {
  id: string;
  companyName: string;
  phoneNumber?: string | null;
  imageUrl?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
}

export interface ClientCategoryDto {
  id: string;
  name?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  displayOrder: number;
}

export interface ClientProductDto {
  id: string;
  categoryId: string;
  providerId: string;
  name: string;
  description?: string | null;
  price: number;
  imageUrl?: string | null;
}

export interface ShoppingCartItemDto {
  id: string;
  productId: string;
  productName: string;
  productImage: string;
  quantity: number;
  price: number;
}

export interface ShoppingCartDto {
  id: string;
  providerId: string;
  providerName: string;
  userId: string;
  items: ShoppingCartItemDto[];
  totalPrice: number;
}

export interface CreatePaymentSessionDto {
  sessionId: string;
  url: string;
  metadata: Record<string, string>;
}

export interface OrderListItemDto {
  id: string;
  providerId: string;
  userId: string;
  orderDateUtc: string;
  totalAmount: number;
  status: OrderStatus | number;
  isHistorical?: boolean;
}

export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderDetailDto {
  id: string;
  providerId: string;
  userId: string;
  orderDateUtc: string;
  totalAmount: number;
  status: OrderStatus | number;
  isHistorical?: boolean;
  notes?: string | null;
  stripeCheckoutSessionId?: string | null;
  items: OrderItemDto[];
}

export interface NotificationDto {
  id: string;
  title: string;
  body: string;
  notificationType: string;
  notificationId?: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface UnreadNotificationCountDto {
  unreadCount: number;
}

export interface ProviderStoreDto {
  id: string;
  companyName: string;
  phoneNumber?: string | null;
  imageUrl?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
}

export interface CategoryDetailDto {
  id: string;
  providerId: string;
  name: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
  imageUrl?: string | null;
  translations?: {
    name: LocalizedText;
    description?: LocalizedText | null;
  } | null;
}

export interface ProductListItemDto {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  sku: string;
  price: number;
  status: number;
  imageUrl?: string | null;
}

export interface ProductDetailDto {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  translations?: {
    name: LocalizedText;
    description?: LocalizedText | null;
  } | null;
  sku: string;
  price: number;
  status: number;
  imageUrl?: string | null;
}

export interface PermissionItemDto {
  name: string;
  action: string;
  description?: string | null;
}

export interface PermissionGroupDto {
  module: string;
  moduleLabel?: string | null;
  permissions: PermissionItemDto[];
}

export interface RoleListItemDto {
  id: string;
  name: string;
  roleType: string | number;
  providerId?: string | null;
  isSystem: boolean;
  usersCount: number;
  permissions: string[];
}

export interface RoleDetailDto {
  id: string;
  name: string;
  names?: LocalizedText | null;
  roleType: string | number;
  providerId?: string | null;
  isSystem: boolean;
  usersCount: number;
  permissions: string[];
}

export interface StaffListItemDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
  roleName: string;
  userType: string | number;
  providerId?: string | null;
  isActive: boolean;
  isSystem: boolean;
}

export interface StaffDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
  roleName: string;
  permissions: string[];
  userType: string | number;
  providerId?: string | null;
  isActive: boolean;
  isSystem: boolean;
}

export interface ProviderAdminDto {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  companyName: string;
  phoneNumber?: string | null;
  serviceId?: string | null;
  serviceName?: string | null;
  isActive: boolean;
  emailConfirmed?: boolean;
  imageUrl?: string | null;
  createdAtUtc: string;
  lastModifiedAtUtc?: string | null;
}

export interface MarketplaceServiceDto {
  id: string;
  code: string;
  isActive: boolean;
  displayOrder: number;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  translations: {
    name: LocalizedText;
    description?: LocalizedText | null;
  };
  createdAtUtc: string;
  lastModifiedAtUtc?: string | null;
}

export interface MarketplaceServiceLookupDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface AdminClientDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  emailConfirmed?: boolean;
  createdAtUtc?: string;
}

export type { AuthResponseDto, ProfileDto, PagedResult };
