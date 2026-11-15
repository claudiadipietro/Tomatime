import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../../services/auth-session.service';
import { authInterceptor } from '../../services/auth.interceptor';
import { MainScreenComponent } from './main-screen-component';
import { LoginComponent } from '../login-component/login-component';

describe('Main screen navigation', () => {
  let http: HttpTestingController;
  let auth: AuthSessionService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter(routes),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  function signIn(expiresAt = new Date(Date.now() + 60_000).toISOString()): void {
    auth.save({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'token',
      expiresAt,
    });
  }

  it('redirects anonymous and expired sessions to login without fetching data', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home', LoginComponent);
    expect(TestBed.inject(Router).url).toBe('/login');
    signIn(new Date(Date.now() - 1000).toISOString());
    await harness.navigateByUrl('/home', LoginComponent);
    expect(TestBed.inject(Router).url).toBe('/login');
    http.expectNone('/api/dashboard');
  });

  it('loads authenticated dashboard data and signs out', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/home', MainScreenComponent);
    const request = http.expectOne('/api/dashboard');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token');
    request.flush({ pomoBalance: 24, nextSession: null, focusMinutes: 50, completedSessions: 2 });
    await harness.fixture.whenStable();
    expect(component.dashboard()?.pomoBalance).toBe(24);
    expect(harness.routeNativeElement?.textContent).toContain('24');
    expect(harness.routeNativeElement?.querySelectorAll('button:disabled').length).toBe(1);
    expect(harness.routeNativeElement?.querySelector('a[href="/sessions/new"]')).toBeTruthy();
    expect(harness.routeNativeElement?.querySelector('a[href="/sessions/plan"]')).toBeTruthy();
    component.logout();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(auth.accessToken).toBeNull();
  });

  it('starts the existing planned session by id and opens its timer', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/home', MainScreenComponent);
    http.expectOne('/api/dashboard').flush({
      pomoBalance: 0,
      nextSession: { id: 45, plannedStartTime: new Date().toISOString(), duration: 50 },
      focusMinutes: 0,
      completedSessions: 0,
    });

    await harness.fixture.whenStable();
    component.startPlanned(45);
    component.startPlanned(45);
    const request = http.expectOne('/api/session-starts/45');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/45/timer');
    http.expectOne('/api/session-runs/45/heartbeat').flush({
      id: 45,
      duration: 50,
      actualStartTime: new Date().toISOString(),
      serverTime: new Date().toISOString(),
    });
    http.expectNone('/api/session-starts');
  });

  it('keeps the planned session visible if starting it fails', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/home', MainScreenComponent);
    http.expectOne('/api/dashboard').flush({
      pomoBalance: 0,
      nextSession: { id: 45, plannedStartTime: new Date().toISOString(), duration: 50 },
      focusMinutes: 0,
      completedSessions: 0,
    });
    component.startPlanned(45);
    http.expectOne('/api/session-starts/45').flush('error', { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(component.startError()).toBe(true);
    expect(component.dashboard()?.nextSession?.id).toBe(45);
  });

  it('shows an error and retries when the dashboard request fails', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/home', MainScreenComponent);
    http.expectOne('/api/dashboard').flush('error', { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeTruthy();
    component.loadDashboard();
    http
      .expectOne('/api/dashboard')
      .flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0 });
    await harness.fixture.whenStable();
    expect(component.error()).toBe(false);
  });

  it('clears the session and redirects after a 401 response', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home', MainScreenComponent);
    http.expectOne('/api/dashboard').flush('', { status: 401, statusText: 'Unauthorized' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(auth.accessToken).toBeNull();
  });

  it('navigates to the dashboard after a successful login', async () => {
    const harness = await RouterTestingHarness.create();
    const login = await harness.navigateByUrl('/login', LoginComponent);
    login.form.setValue({ email: 'jane@example.com', password: 'password' });
    login.submit();
    http.expectOne('/api/auth/login').flush({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/home');
    http
      .expectOne('/api/dashboard')
      .flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0 });
  });
});
