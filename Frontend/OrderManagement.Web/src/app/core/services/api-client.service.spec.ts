import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ApiError, apiErrorMessage } from '../models/api.model';
import { ApiClient } from './api-client.service';

describe('ApiClient response contract', () => {
  let api: ApiClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ApiClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts JSON to the current origin and unwraps value', async () => {
    const result = firstValueFrom(api.post<{ id: number }>('orders/create', { code: 'DH01' }));
    const request = http.expectOne('/api/orders/create');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ code: 'DH01' });
    request.flush({ status: 201, value: { id: 7 }, message: 'Đã tạo.' }, { status: 201, statusText: 'Created' });
    await expect(result).resolves.toEqual({ id: 7 });
  });

  it('sends an empty object for requests without parameters', async () => {
    const result = firstValueFrom(api.post('orders/lookups'));
    const request = http.expectOne('/api/orders/lookups');
    expect(request.request.body).toEqual({});
    request.flush({ status: 200, value: {}, message: '' });
    await result;
  });

  it('retains validation errors and traceId for a rejected request', async () => {
    const result = firstValueFrom(api.post<never>('orders/create')).catch(error => error as ApiError);
    http.expectOne('/api/orders/create').flush({
      status: 400, value: null, message: 'Dữ liệu chưa hợp lệ.',
      errors: { quantity: ['Số lượng phải lớn hơn 0.'] }, traceId: 'request-123',
    }, { status: 400, statusText: 'Bad Request' });
    const error = await result;
    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.traceId).toBe('request-123');
    expect(apiErrorMessage(error)).toBe('Dữ liệu chưa hợp lệ. Số lượng phải lớn hơn 0.');
  });

  it.each([
    { status: 200, message: 'Missing value' },
    { status: 500, value: null, message: 'HTTP/envelope mismatch' },
    { status: 200, value: {}, message: 'Malformed errors', errors: { code: 'not an array' } },
  ])('rejects malformed or inconsistent success envelopes: %j', async response => {
    const result = firstValueFrom(api.post<never>('orders/search')).catch(error => error as ApiError);
    http.expectOne('/api/orders/search').flush(response);
    expect((await result).message).toContain('Phản hồi máy chủ không hợp lệ');
  });

  it('converts unexpected HTML server errors into a safe message', async () => {
    const result = firstValueFrom(api.post<never>('orders/search')).catch(error => error as ApiError);
    http.expectOne('/api/orders/search').flush('<html>Internal configuration error</html>',
      { status: 502, statusText: 'Bad Gateway' });
    const error = await result;
    expect(error.status).toBe(502);
    expect(error.message).not.toContain('html');
    expect(error.message).toContain('Máy chủ chưa thể xử lý');
  });

  it('reports a connection failure', async () => {
    const result = firstValueFrom(api.post<never>('orders/search')).catch(error => error as ApiError);
    http.expectOne('/api/orders/search').error(new ProgressEvent('error'));
    expect((await result).message).toContain('Không thể kết nối');
  });
});
