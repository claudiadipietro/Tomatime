import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { vi } from 'vitest';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../../services/auth-session.service';
import { authInterceptor } from '../../services/auth.interceptor';
import { LoginComponent } from '../login-component/login-component';
import { PlanSessionComponent } from './plan-session-component';

describe('Plan session', () => {
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
    auth.clear();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/sessions/plan', LoginComponent);
    await new Promise((resolve) => setTimeout(resolve, 0));
    http.expectOne('/api/auth/refresh').flush('', { status: 401, statusText: 'Unauthorized' });
    await navigation;
    expect(TestBed.inject(Router).url).toBe('/login');
    http.expectNone('/api/sessions');
  });

  it('requires a valid future date and time', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/plan', PlanSessionComponent);
    component.submit();
    expect(component.form.controls.date.touched).toBe(true);
    component.form.setValue({ date: '2020-01-01', time: '10:00' });
    component.submit();
    expect(component.errorKey()).toBe('planning.invalidTime');
    component.form.setValue({ date: '2031-02-31', time: '10:00' });
    component.submit();
    expect(component.errorKey()).toBe('planning.invalidTime');
    http.expectNone('/api/sessions');
  });

  it('limits the date picker to today and updates after midnight', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/plan', PlanSessionComponent);
    vi.useFakeTimers();
    try {
      vi.setSystemTime(new Date(2026, 2, 16, 23, 59, 30));
      component.refreshToday();
      harness.detectChanges();
      const input = harness.routeNativeElement!.querySelector<HTMLInputElement>('#planned-date')!;
      expect(input.min).toBe('2026-03-16');
      component.form.setValue({ date: '2026-03-16', time: '23:59' });
      component.submit();
      expect(component.errorKey()).toBe('planning.invalidTime');
      http.expectNone('/api/sessions');
      vi.setSystemTime(new Date(2026, 2, 17, 0, 0, 1));
      component.refreshToday();
      harness.detectChanges();
      expect(input.min).toBe('2026-03-17');
      component.form.setValue({ date: '2026-03-17', time: '00:01' });
      component.submit();
      http
        .expectOne('/api/sessions')
        .flush({
          id: 2,
          duration: 50,
          plannedStartTime: new Date(2026, 2, 17, 0, 1).toISOString(),
        });
    } finally {
      vi.useRealTimers();
    }
  });

  it('updates the duration and schedule from the visible controls', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/plan', PlanSessionComponent);
    const root = harness.routeNativeElement!;
    const date = root.querySelector<HTMLInputElement>('#planned-date')!;
    const time = root.querySelector<HTMLInputElement>('#planned-time')!;
    date.value = '2099-12-10';
    date.dispatchEvent(new Event('input'));
    time.value = '14:30';
    time.dispatchEvent(new Event('input'));
    root.querySelector<HTMLButtonElement>('.adjust-button:last-child')!.click();
    harness.detectChanges();
    expect(component.form.getRawValue()).toEqual({ date: '2099-12-10', time: '14:30' });
    expect(component.duration()).toBe(75);
    root.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    const request = http.expectOne('/api/sessions');
    expect(request.request.body.duration).toBe(75);
    request.flush({ id: 2, duration: 75, plannedStartTime: request.request.body.plannedStartTime });
  });

  it('sends local date and time as UTC once and displays confirmation', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/plan', PlanSessionComponent);
    component.duration.set(75);
    component.form.setValue({ date: '2099-12-10', time: '14:30' });
    component.submit();
    component.submit();
    const request = http.expectOne('/api/sessions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      duration: 75,
      plannedStartTime: new Date('2099-12-10T14:30').toISOString(),
    });
    expect(request.request.headers.has('Authorization')).toBe(true);
    request.flush({ id: 2, duration: 75, plannedStartTime: request.request.body.plannedStartTime });
    await harness.fixture.whenStable();
    expect(component.planned()).toBe(true);
    expect(harness.routeNativeElement?.querySelector('[role="status"]')).toBeTruthy();
    component.submit();
    http.expectNone('/api/sessions');
  });

  it('surfaces server validation and allows retrying', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/plan', PlanSessionComponent);
    component.form.setValue({ date: '2099-12-10', time: '14:30' });
    component.submit();
    http.expectOne('/api/sessions').flush({}, { status: 400, statusText: 'Bad Request' });
    await harness.fixture.whenStable();
    expect(component.errorKey()).toBe('planning.checkDetails');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeTruthy();
    component.submit();
    http
      .expectOne('/api/sessions')
      .flush({ id: 2, duration: 50, plannedStartTime: '2099-12-10T14:30:00Z' });
    expect(component.planned()).toBe(true);
  });
});
