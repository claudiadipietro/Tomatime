import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../../services/auth-session.service';
import { authInterceptor } from '../../services/auth.interceptor';
import { LoginComponent } from '../login-component/login-component';
import { NewSessionComponent } from './new-session-component';

describe('New session', () => {
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

  function signIn(): void {
    auth.save({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
  }

  it('requires authentication', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/new', LoginComponent);
    expect(TestBed.inject(Router).url).toBe('/login');
    http.expectNone('/api/sessions');
  });

  it('adjusts the duration in 25-minute blocks within the chosen limits', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/new', NewSessionComponent);
    expect(component.duration()).toBe(50);
    expect(component.blocks().length).toBe(2);
    component.changeDuration(-25);
    component.changeDuration(-25);
    expect(component.duration()).toBe(25);
    for (let i = 0; i < 20; i++) component.changeDuration(25);
    expect(component.duration()).toBe(300);
    expect(component.blocks().length).toBe(12);
  });

  it('creates exactly one spontaneous session and opens the timer', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/new', NewSessionComponent);
    component.changeDuration(25);
    component.start();
    component.start();
    const request = http.expectOne('/api/sessions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ duration: 75 });
    expect(request.request.headers.has('Authorization')).toBe(true);
    request.flush({ id: 1, duration: 75, actualStartTime: new Date().toISOString() });
    await harness.fixture.whenStable();
    expect(component.started()).toBe(true);
    expect(TestBed.inject(Router).url).toBe('/sessions/1/timer');
    http.expectOne('/api/sessions/1/heartbeat').flush({
      id: 1,
      duration: 75,
      actualStartTime: new Date().toISOString(),
      serverTime: new Date().toISOString(),
    });
    component.start();
    http.expectNone('/api/sessions');
  });

  it('shows errors and allows retry', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/new', NewSessionComponent);
    component.start();
    http.expectOne('/api/sessions').flush('error', { status: 500, statusText: 'Error' });
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeTruthy();
    expect(component.submitting()).toBe(false);
    component.start();
    http
      .expectOne('/api/sessions')
      .flush({ id: 2, duration: 50, actualStartTime: new Date().toISOString() });
    expect(component.started()).toBe(true);
    await harness.fixture.whenStable();
    http.expectOne('/api/sessions/2/heartbeat').flush({
      id: 2,
      duration: 50,
      actualStartTime: new Date().toISOString(),
      serverTime: new Date().toISOString(),
    });
  });
});
