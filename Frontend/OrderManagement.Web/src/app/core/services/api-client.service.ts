import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';
import { ApiError, ApiResponse } from '../models/api.model';

/** The only transport used by business services. IIS and development use the same API paths. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  post<T>(path: string, body: object = {}): Observable<T> {
    return this.http.post<unknown>(`/api/${path}`, body, { observe: 'response' }).pipe(
      map(response => {
        if (!isApiResponse(response.body) || response.body.status !== response.status) {
          throw new ApiError('Phản hồi máy chủ không hợp lệ. Vui lòng thử lại.', response.status);
        }
        return response.body.value as T;
      }),
      catchError((error: unknown) => throwError(() => normalizeApiError(error))),
    );
  }
}

function isApiResponse(value: unknown): value is ApiResponse<unknown> {
  if (!value || typeof value !== 'object') return false;
  const response = value as Record<string, unknown>;
  return Number.isInteger(response['status'])
    && typeof response['message'] === 'string'
    && Object.hasOwn(response, 'value')
    && (response['traceId'] === undefined || typeof response['traceId'] === 'string')
    && (response['errors'] === undefined || isValidationErrors(response['errors']));
}

function isValidationErrors(value: unknown): value is Record<string, string[]> {
  return !!value && typeof value === 'object' && !Array.isArray(value)
    && Object.values(value).every(messages => Array.isArray(messages)
      && messages.every(message => typeof message === 'string'));
}

function normalizeApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (!(error instanceof HttpErrorResponse)) return new ApiError('Đã xảy ra lỗi. Vui lòng thử lại.', 0);
  if (isApiResponse(error.error) && error.error.status === error.status) {
    return new ApiError(error.error.message || fallbackMessage(error.status), error.status,
      error.error.errors, error.error.traceId);
  }
  return new ApiError(fallbackMessage(error.status), error.status);
}

function fallbackMessage(status: number): string {
  switch (status) {
    case 0: return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối và thử lại.';
    case 400: case 422: return 'Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại.';
    case 401: return 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.';
    case 403: return 'Bạn không có quyền thực hiện thao tác này.';
    case 404: return 'Không tìm thấy dữ liệu hoặc chức năng được yêu cầu.';
    case 405: case 415: return 'Yêu cầu không đúng định dạng được hỗ trợ.';
    case 409: return 'Dữ liệu đã thay đổi hoặc đang được sử dụng. Vui lòng tải lại.';
    case 429: return 'Bạn gửi quá nhiều yêu cầu. Vui lòng thử lại sau.';
    default: return 'Máy chủ chưa thể xử lý yêu cầu. Vui lòng thử lại sau.';
  }
}
