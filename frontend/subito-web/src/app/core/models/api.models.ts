export interface ApiResponse<T = unknown> {
  success: boolean;
  statusCode: number;
  message: string;
  errors: string[];
  fieldErrors?: Record<string, string[]>;
  data: T | null;
  traceId: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface LocalizedText {
  en: string;
  it?: string | null;
  ar?: string | null;
}

export type UserType = 'Client' | 'Admin' | 'Provider' | number;

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  userType: UserType;
  roles: string[];
}

export interface AuthResponseDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  user: UserDto;
}

export interface ProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  userType: UserType;
  emailConfirmed: boolean;
}

export interface OtpSentDto {
  message: string;
  developmentOtp?: string | null;
}

export type Portal = 'client' | 'provider' | 'admin';

export enum OrderStatus {
  New = 0,
  Preparing = 2,
  Ready = 3,
}

export const ORDER_STATUS_LABELS: Record<number, string> = {
  [OrderStatus.New]: 'order.status.new',
  [OrderStatus.Preparing]: 'order.status.preparing',
  [OrderStatus.Ready]: 'order.status.ready',
};
