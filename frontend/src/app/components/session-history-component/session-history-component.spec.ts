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
import { MainScreenComponent } from '../main-screen-component/main-screen-component';
import { SessionHistoryComponent } from './session-history-component';

describe('Session history', () => {
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
    TestBed.inject(AuthSessionService).clear();
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/sessions/history', LoginComponent);
    await new Promise((resolve) => setTimeout(resolve, 0));
    http.expectOne('/api/auth/refresh').flush('', { status: 401, statusText: 'Unauthorized' });
    await navigation;
    http.expectNone('/api/sessions/history');
  });

  it('opens history from the menu and links to the profile', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home', MainScreenComponent);
    http
      .expectOne('/api/dashboard')
      .flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0 });
    harness.detectChanges();
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('.menu-toggle')!.click();
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('a[href="/profile"]')).toBeTruthy();
    harness
      .routeNativeElement!.querySelector<HTMLAnchorElement>('a[href="/sessions/history"]')!
      .click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/sessions/history');
    const request = http.expectOne('/api/sessions/history');
    expect(request.request.headers.has('Authorization')).toBe(true);
    request.flush([]);
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement!.textContent).toContain('history.empty');
  });

  it('groups completed and cancelled sessions by local start date', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/history', SessionHistoryComponent);
    const today = new Date();
    const yesterday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1, 23, 45);
    const sameDay = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 8);
    http.expectOne('/api/sessions/history').flush([
      {
        id: 2,
        duration: 50,
        status: 'Completed',
        actualStartTime: sameDay.toISOString(),
        endedAt: new Date(sameDay.getTime() + 55 * 60000).toISOString(),
        pomosEarned: 4,
      },
      {
        id: 1,
        duration: 25,
        status: 'Cancelled',
        actualStartTime: yesterday.toISOString(),
        endedAt: null,
        pomosEarned: 0,
      },
      {
        id: 3,
        duration: 25,
        status: 'Cancelled',
        actualStartTime: null,
        endedAt: null,
        pomosEarned: 0,
      },
    ]);
    harness.detectChanges();
    expect(component.groups().length).toBe(3);
    expect(component.isToday(component.groups()[0].date)).toBe(true);
    expect(harness.routeNativeElement!.querySelectorAll('.session-card').length).toBe(3);
    expect(harness.routeNativeElement!.textContent).toContain('history.cancelled');
    expect(harness.routeNativeElement!.textContent).toContain('history.plannedDuration');
    expect(harness.routeNativeElement!.textContent).toContain('history.unknownDate');
  });

  it('groups unstarted cancellations by planned date without showing a false time range', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/history', SessionHistoryComponent);
    const planned = new Date(2026, 10, 22, 16);
    const cancelled = new Date(2026, 10, 21, 14);
    http.expectOne('/api/sessions/history').flush([{
      id: 4, duration: 50, status: 'Cancelled', actualStartTime: null,
      plannedStartTime: planned.toISOString(), endedAt: cancelled.toISOString(), pomosEarned: 0,
    }]);
    harness.detectChanges();
    expect(component.groups()[0].date).toEqual(planned);
    const text = harness.routeNativeElement!.querySelector('.session-card')!.textContent!;
    expect(text).toContain('history.plannedStart');
    expect(text).toContain(component.formatTime(planned.toISOString()));
    expect(text).not.toContain('history.unknownDate');
    expect(text).not.toContain('–');
  });

  it('offers retry on failure and redirects on expired authentication', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/history', SessionHistoryComponent);
    http.expectOne('/api/sessions/history').flush('', { status: 500, statusText: 'Error' });
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('[role="alert"]')).toBeTruthy();
    component.load();
    http.expectOne('/api/sessions/history').flush('', { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush('', { status: 401, statusText: 'Unauthorized' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(TestBed.inject(AuthSessionService).accessToken).toBeNull();
  });
});
