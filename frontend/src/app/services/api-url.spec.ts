import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_ORIGIN, apiUrl } from './api-url';
import { authInterceptor } from './auth.interceptor';
import { AuthSessionService } from './auth-session.service';

describe('API URL configuration', () => {
  const origin = 'https://api.example.test';
  let http: HttpClient;
  let requests: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: API_ORIGIN, useValue: origin },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthSessionService).save({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'test-token',
      expiresAt: new Date(Date.now() + 3600000).toISOString(),
    });
  });

  afterEach(() => {
    requests.verify();
    sessionStorage.clear();
  });

  it('redirects authenticated API requests to the configured origin', () => {
    http.get('/api/dashboard?period=week').subscribe();
    const request = requests.expectOne(`${origin}/api/dashboard?period=week`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({});
  });

  it('redirects authentication requests without adding a token', () => {
    http.post('/api/auth/login', {}).subscribe();
    const request = requests.expectOne(`${origin}/api/auth/login`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('leaves other URLs untouched and does not add a token', () => {
    for (const url of ['/i18n/en.json', 'https://other.example.test/api/dashboard']) {
      http.get(url).subscribe();
      const request = requests.expectOne(url);
      expect(request.request.headers.has('Authorization')).toBe(false);
      request.flush({});
    }
  });

  it('preserves local URLs and resolves API URLs against the configured origin', () => {
    expect(apiUrl('/api/sessions/12/cancel', '')).toBe('/api/sessions/12/cancel');
    expect(apiUrl('/api/sessions/12/cancel', origin)).toBe(`${origin}/api/sessions/12/cancel`);
    expect(() => apiUrl('//other.example.test/api', origin)).toThrow();
  });
});
