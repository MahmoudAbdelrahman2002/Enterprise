import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map, timeout } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.models';
import { I18nService } from './i18n.service';
import { ToastService } from './toast.service';

export type Query = Record<string, string | number | boolean | null | undefined>;

export type RequestOptions = {
  silent?: boolean;
};

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiBaseUrl;

  constructor(
    private readonly http: HttpClient,
    private readonly i18n: I18nService,
    private readonly toast: ToastService
  ) {}

  get<T>(path: string, query?: Query, options?: RequestOptions): Observable<T> {
    return this.request<T>('GET', path, undefined, query, options);
  }

  post<T>(path: string, body?: unknown, query?: Query): Observable<T> {
    return this.request<T>('POST', path, body, query);
  }

  put<T>(path: string, body?: unknown): Observable<T> {
    return this.request<T>('PUT', path, body);
  }

  patch<T>(path: string, body?: unknown): Observable<T> {
    return this.request<T>('PATCH', path, body);
  }

  delete<T>(path: string): Observable<T> {
    return this.request<T>('DELETE', path);
  }

  upload<T>(path: string, file: File, fieldName = 'file'): Observable<T> {
    const form = new FormData();
    form.append(fieldName, file, file.name);
    return this.http
      .post<ApiResponse<T>>(`${this.base}${path}`, form, {
        headers: new HttpHeaders({ 'Accept-Language': this.i18n.lang() }),
      })
      .pipe(
        map((res) => this.unwrap(res)),
        catchError((err) => this.handleError(err))
      );
  }

  private request<T>(
    method: string,
    path: string,
    body?: unknown,
    query?: Query,
    options?: RequestOptions
  ): Observable<T> {
    let params = new HttpParams();
    if (query) {
      Object.entries(query).forEach(([k, v]) => {
        if (v !== null && v !== undefined && v !== '') {
          params = params.set(k, String(v));
        }
      });
    }

    return this.http
      .request<ApiResponse<T>>(method, `${this.base}${path}`, {
        body,
        params,
        headers: new HttpHeaders({
          'Content-Type': 'application/json',
          'Accept-Language': this.i18n.lang(),
        }),
      })
      .pipe(
        timeout(20000),
        map((res) => this.unwrap(res)),
        catchError((err) => this.handleError(err, options?.silent === true))
      );
  }

  private unwrap<T>(res: ApiResponse<T>): T {
    const body = (res ?? {}) as unknown as Record<string, unknown>;
    const success = body['success'] ?? body['Success'];
    if (success === false) {
      const errors = (body['errors'] ?? body['Errors']) as string[] | undefined;
      const message = String(body['message'] ?? body['Message'] ?? '');
      const msg = errors?.length ? errors.join('\n') : message;
      throw { message: msg, statusCode: body['statusCode'] ?? body['StatusCode'], response: res };
    }
    if (success === undefined && !('data' in body) && !('Data' in body)) {
      return res as T;
    }
    return (body['data'] ?? body['Data']) as T;
  }

  private handleError(err: unknown, silent = false) {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiResponse | undefined;
      const msg =
        body?.errors?.length
          ? body.errors.join('\n')
          : body?.message || err.message || this.i18n.t('errors.generic', 'Something went wrong');
      if (!silent && err.status !== 401) {
        this.toast.error(msg);
      }
      return throwError(() => ({ message: msg, statusCode: err.status, response: body }));
    }
    const anyErr = err as { message?: string };
    if (!silent && anyErr?.message) {
      this.toast.error(anyErr.message);
    }
    return throwError(() => err);
  }
}
