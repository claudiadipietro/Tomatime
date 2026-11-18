import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AuthSessionService } from './auth-session.service';
import { AuthResponse } from '../models/auth.models';

describe('AuthService', () => {
  let authService: AuthService;
  let authSession: AuthSessionService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    authService = TestBed.inject(AuthService);
    authSession = TestBed.inject(AuthSessionService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('sends the backend registration payload and stores the returned session', () => {
    const request = {
      name: 'Jane Doe',
      email: 'jane@example.com',
      password: 'secure-password',
    };
    const response: AuthResponse = {
      userId: 7,
      name: request.name,
      email: request.email,
      accessToken: 'jwt-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    };

    authService.register(request).subscribe();

    const httpRequest = httpTestingController.expectOne('/api/auth/register');
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    httpRequest.flush(response);

    expect(authSession.currentUser()).toEqual({
      id: 7,
      name: 'Jane Doe',
      email: 'jane@example.com',
    });
    expect(authSession.accessToken).toBe('jwt-token');
    expect(JSON.parse(sessionStorage.getItem('tomatime.auth') ?? '{}').accessToken).toBe(
      'jwt-token',
    );
  });

  it('sends the backend login payload', () => {
    const request = { email: 'jane@example.com', password: 'secure-password' };

    authService.login(request).subscribe();

    const httpRequest = httpTestingController.expectOne('/api/auth/login');
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    expect(httpRequest.request.withCredentials).toBe(true);
    httpRequest.flush({
      userId: 7,
      name: 'Jane Doe',
      email: request.email,
      accessToken: 'jwt-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
  });

  it('restores a closed PWA session through the refresh cookie', () => {
    authService.restoreSession().subscribe();

    const request = httpTestingController.expectOne('/api/auth/refresh');
    expect(request.request.withCredentials).toBe(true);
    request.flush({
      userId: 7,
      name: 'Jane Doe',
      email: 'jane@example.com',
      accessToken: 'renewed-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });

    expect(authSession.accessToken).toBe('renewed-jwt');
  });
});
