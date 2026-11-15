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
import { PlanSessionComponent } from '../plan-session-component/plan-session-component';
import { SessionDetailComponent } from '../session-detail-component/session-detail-component';
import { SessionCalendarComponent } from './session-calendar-component';

describe('Session calendar', () => {
  let http: HttpTestingController;

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
  });
  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });
  function signIn(): void {
    TestBed.inject(AuthSessionService).save({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
  }
  function requestMonth(year: number, month: number) {
    const request = http.expectOne((r) => r.url === '/api/session-calendar');
    expect(request.request.headers.has('Authorization')).toBe(true);
    expect(request.request.params.get('from')).toBe(new Date(year, month - 1, 1).toISOString());
    expect(request.request.params.get('to')).toBe(new Date(year, month, 1).toISOString());
    return request;
  }

  it('requires authentication', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/calendar', LoginComponent);
    http.expectNone('/api/session-calendar');
  });

  it('prevents planning on past days while allowing history navigation and future dates', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/sessions/calendar?date=2026-03-15',
      SessionCalendarComponent,
    );
    requestMonth(2026, 3).flush([]);
    vi.useFakeTimers();
    try {
      vi.setSystemTime(new Date(2026, 2, 16, 23, 59, 30));
      component.refreshToday();
      harness.detectChanges();
      expect(component.selectedDateIsPast()).toBe(true);
      expect(
        harness.routeNativeElement!.querySelector<HTMLButtonElement>('button.primary')!.disabled,
      ).toBe(true);
      expect(harness.routeNativeElement!.textContent).toContain('calendar.pastDate');
      expect(harness.routeNativeElement!.querySelectorAll('button.day').length).toBe(42);
      component.select(component.days().find((day) => day.key === '2026-03-16')!);
      harness.detectChanges();
      expect(
        harness.routeNativeElement!.querySelector<HTMLButtonElement>('button.primary')!.disabled,
      ).toBe(false);
      vi.setSystemTime(new Date(2026, 2, 17, 0, 0, 1));
      component.refreshToday();
      harness.detectChanges();
      expect(
        harness.routeNativeElement!.querySelector<HTMLButtonElement>('button.primary')!.disabled,
      ).toBe(true);
      component.select(component.days().find((day) => day.key === '2026-03-18')!);
      harness.detectChanges();
      expect(
        harness.routeNativeElement!.querySelector<HTMLButtonElement>('button.primary')!.disabled,
      ).toBe(false);
    } finally {
      vi.useRealTimers();
    }
  });

  it('groups sessions by local start date, opens a past session, and returns to selected date', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/sessions/calendar?date=2026-03-15',
      SessionCalendarComponent,
    );
    const start = new Date(2026, 2, 15, 23, 45).toISOString();
    requestMonth(2026, 3).flush([
      {
        id: 11,
        duration: 50,
        status: 'Completed',
        plannedStartTime: new Date(2026, 2, 14, 10).toISOString(),
        actualStartTime: start,
        pomosEarned: 4,
      },
      {
        id: 12,
        duration: 25,
        status: 'Planned',
        plannedStartTime: new Date(2026, 2, 15, 11).toISOString(),
        actualStartTime: null,
        pomosEarned: 0,
      },
      {
        id: 13,
        duration: 25,
        status: 'Cancelled',
        plannedStartTime: new Date(2026, 2, 16, 11).toISOString(),
        actualStartTime: null,
        pomosEarned: 0,
      },
    ]);
    harness.detectChanges();
    expect(component.selectedSessions().map((s) => s.id)).toEqual([11, 12]);
    expect(component.days().find((d) => d.key === '2026-03-15')?.hasSessions).toBe(true);
    expect(harness.routeNativeElement!.querySelectorAll('.session-card').length).toBe(2);
    harness.routeNativeElement!.querySelector<HTMLAnchorElement>('.detail-link')!.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/history/11?from=calendar&date=2026-03-15');
    http.expectOne('/api/session-history/11').flush({
      id: 11,
      status: 'Completed',
      duration: 50,
      actualStartTime: start,
      endedAt: null,
      pomosEarned: 4,
    });
    harness.detectChanges();
    harness.routeNativeElement!.querySelector<HTMLAnchorElement>('.primary')!.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/calendar?date=2026-03-15');
    requestMonth(2026, 3).flush([]);
  });

  it('navigates months, selects adjacent dates and pre-fills planning from the selected day', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/sessions/calendar?date=2099-12-10',
      SessionCalendarComponent,
    );
    requestMonth(2099, 12).flush([]);
    component.changeMonth(1);
    expect(component.selectedKey()).toBe('2100-01-01');
    requestMonth(2100, 1).flush([]);
    const day = component.days().find((d) => d.key === '2100-02-01')!;
    component.select(day);
    requestMonth(2100, 2).flush([]);
    harness.detectChanges();
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('.primary')!.click();
    await harness.fixture.whenStable();
    const plan = harness.routeDebugElement!.componentInstance as PlanSessionComponent;
    expect(plan.form.controls.date.value).toBe('2100-02-01');
    expect(TestBed.inject(Router).url).toContain('from=calendar');
    plan.form.controls.time.setValue('10:30');
    plan.submit();
    const request = http.expectOne('/api/session-plans');
    expect(request.request.body.plannedStartTime).toBe(new Date(2100, 1, 1, 10, 30).toISOString());
    request.flush({ id: 8, duration: 50, plannedStartTime: request.request.body.plannedStartTime });
    harness.detectChanges();
    harness.routeNativeElement!.querySelector<HTMLAnchorElement>('.primary-button')!.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/calendar?date=2100-02-01');
    requestMonth(2100, 2).flush([]);
  });

  it('handles retry, starts a planned session, and opens the timer', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/sessions/calendar?date=2099-12-10',
      SessionCalendarComponent,
    );
    requestMonth(2099, 12).flush('', { status: 500, statusText: 'Error' });
    expect(component.error()).toBe(true);
    component.load();
    requestMonth(2099, 12).flush([
      {
        id: 12,
        duration: 25,
        status: 'Planned',
        plannedStartTime: new Date(2099, 11, 10, 10).toISOString(),
        actualStartTime: null,
        pomosEarned: 0,
      },
    ]);
    component.start(component.selectedSessions()[0]);
    component.start(component.selectedSessions()[0]);
    http.expectOne('/api/session-starts/12').flush(null, { status: 204, statusText: 'No Content' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/12/timer');
    http.expectOne('/api/session-runs/12/heartbeat').flush({
      id: 12,
      duration: 25,
      actualStartTime: new Date().toISOString(),
      serverTime: new Date().toISOString(),
    });
  });

  it('ignores malformed dates and redirects on unauthorized requests', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      '/sessions/calendar?date=2026-02-31',
      SessionCalendarComponent,
    );
    const now = new Date();
    requestMonth(now.getFullYear(), now.getMonth() + 1).flush('', {
      status: 401,
      statusText: 'Unauthorized',
    });
    await harness.fixture.whenStable();
    expect(component.selected().getMonth()).toBe(now.getMonth());
    expect(TestBed.inject(Router).url).toBe('/login');
  });
});
