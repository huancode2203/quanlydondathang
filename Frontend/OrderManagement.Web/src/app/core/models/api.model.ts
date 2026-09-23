export interface ApiResponse<T> {
  status: number;
  value: T | null;
  message: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/** Common search fields, shared by feature-specific request models. */
export interface SearchFilter {
  keyword?: string;
}

export interface PagedFilter extends SearchFilter {
  page: number;
  pageSize: number;
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly errors: Record<string, string[]> = {},
    readonly traceId?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function apiErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) return 'Đã xảy ra lỗi. Vui lòng thử lại.';
  return [...new Set([error.message, ...Object.values(error.errors).flat()])]
    .filter(Boolean)
    .join(' ');
}
