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
import { SessionHistoryComponent } from '../session-history-component/session-history-component';
import { SessionDetailComponent } from './session-detail-component';

describe('Session details', () => {
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

  it('requires authentication', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/history/12', LoginComponent);
    http.expectNone('/api/session-history/12');
  });

  it('opens the selected history record and returns to history', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/history', SessionHistoryComponent);
    http.expectOne('/api/session-history').flush([
      {
        id: 12,
        status: 'Completed',
        duration: 50,
        actualStartTime: '2026-11-15T10:00:00Z',
        endedAt: '2026-11-15T10:55:00Z',
        pomosEarned: 4,
      },
    ]);
    harness.detectChanges();
    const link = harness.routeNativeElement!.querySelector<HTMLAnchorElement>('.session-card')!;
    expect(link.getAttribute('href')).toBe('/sessions/history/12');
    link.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/history/12');
    const request = http.expectOne('/api/session-history/12');
    expect(request.request.headers.has('Authorization')).toBe(true);
    request.flush({
      id: 12,
      status: 'Completed',
      duration: 50,
      actualStartTime: '2026-11-15T10:00:00Z',
      endedAt: '2026-11-15T10:55:00Z',
      pomosEarned: 4,
    });
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('sessionDetail.completed');
    expect(harness.routeNativeElement!.querySelector('.detail-card')).toBeTruthy();
    harness
      .routeNativeElement!.querySelector<HTMLAnchorElement>('a[href="/sessions/history"]')!
      .click();
    await harness.fixture.whenStable();
    http.expectOne('/api/session-history').flush([]);
    expect(TestBed.inject(Router).url).toBe('/sessions/history');
  });

  it('displays cancelled sessions with planned duration and unavailable timestamps', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/history/13', SessionDetailComponent);
    http.expectOne('/api/session-history/13').flush({
      id: 13,
      status: 'Cancelled',
      duration: 25,
      actualStartTime: null,
      endedAt: null,
      pomosEarned: 0,
    });
    harness.detectChanges();
    const text = harness.routeNativeElement!.textContent!;
    expect(text).toContain('sessionDetail.cancelled');
    expect(text).toContain('history.plannedDuration');
    expect(text).toContain('history.unknownDate');
    expect(text).toContain('sessionDetail.timeUnknown');
  });

  it('displays an unavailable end time and dates spanning midnight', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/history/13', SessionDetailComponent);
    const start = new Date(2026, 10, 15, 23, 50).toISOString();
    const end = new Date(2026, 10, 16, 0, 20).toISOString();
    expect(component.isDifferentDay(start, end)).toBe(true);
    http.expectOne('/api/session-history/13').flush({
      id: 13,
      status: 'Cancelled',
      duration: 25,
      actualStartTime: start,
      endedAt: null,
      pomosEarned: 0,
    });
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('sessionDetail.endUnknown');
  });

  it('cancels the previous request when the route changes', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/history/12', SessionDetailComponent);
    const previous = http.expectOne('/api/session-history/12');
    const component = await harness.navigateByUrl('/sessions/history/13', SessionDetailComponent);
    expect(previous.cancelled).toBe(true);
    http.expectOne('/api/session-history/13').flush({
      id: 13,
      status: 'Cancelled',
      duration: 25,
      actualStartTime: null,
      endedAt: null,
      pomosEarned: 0,
    });
    expect(component.session()?.id).toBe(13);
  });

  it('handles missing sessions, failures, retries and invalid ids', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/history/12', SessionDetailComponent);
    http.expectOne('/api/session-history/12').flush('', { status: 500, statusText: 'Error' });
    expect(component.error()).toBe('error');
    component.load();
    http.expectOne('/api/session-history/12').flush('', { status: 404, statusText: 'Not Found' });
    expect(component.error()).toBe('notFound');
    await harness.navigateByUrl('/sessions/history/invalid', SessionDetailComponent);
    expect(component.error()).toBe('notFound');
    http.expectNone('/api/session-history/invalid');
  });

  it('clears authentication on 401', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sessions/history/12', SessionDetailComponent);
    http
      .expectOne('/api/session-history/12')
      .flush('', { status: 401, statusText: 'Unauthorized' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(TestBed.inject(AuthSessionService).accessToken).toBeNull();
  });
});
