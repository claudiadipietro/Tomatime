import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../../services/auth-session.service';
import { authInterceptor } from '../../services/auth.interceptor';
import { TimerComponent } from './timer-component';

describe('Session timer', () => {
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
    TestBed.inject(AuthSessionService).save({
      userId: 7,
      name: 'Jane',
      email: 'jane@example.com',
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 3600000).toISOString(),
    });
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  async function open(elapsedSeconds: number) {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/12/timer', TimerComponent);
    const dialog = harness.routeNativeElement!.querySelector('dialog')!;
    dialog.showModal = () => dialog.setAttribute('open', '');
    dialog.close = () => dialog.removeAttribute('open');
    const now = Date.now();
    http.expectOne('/api/session-runs/12/heartbeat').flush({
      id: 12,
      duration: 50,
      actualStartTime: new Date(now - elapsedSeconds * 1000).toISOString(),
      serverTime: new Date(now).toISOString(),
    });
    return { harness, component };
  }

  it('displays the focus and break phases from server time', async () => {
    const { component } = await open(1500);
    expect(component.isBreak()).toBe(true);
    expect(component.remaining()).toBe('05:00');
    expect(component.block()).toBe(1);
    expect(component.totalSeconds()).toBe(3300);
  });

  it('completes only after the focus blocks and break and displays the awarded balance', async () => {
    const { harness, component } = await open(3300);
    const request = http.expectOne('/api/session-runs/12/complete');
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('.confetti')).toBeNull();
    request.flush({ pomosEarned: 4 });
    harness.detectChanges();
    expect(component.state()).toBe('completed');
    expect(component.earned()).toBe(4);
    expect(harness.routeNativeElement?.querySelectorAll('.confetti-piece').length).toBe(42);
    expect(
      harness.routeNativeElement?.querySelector('.confetti')?.getAttribute('aria-hidden'),
    ).toBe('true');
    expect(component.leave()).toBe(true);
  });

  it('cancels before navigating away and blocks navigation if cancellation fails', async () => {
    const { harness, component } = await open(10);
    const router = TestBed.inject(Router);
    const first = router.navigateByUrl('/home');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(harness.routeNativeElement!.querySelector('dialog')!.open).toBe(true);
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('dialog .cancel')!.click();
    http.expectOne('/api/session-runs/12/cancel').flush('', { status: 500, statusText: 'Error' });
    expect(await first).toBe(false);
    expect(component.error()).toBe('timer.cancelError');
    const second = router.navigateByUrl('/home');
    await new Promise((resolve) => setTimeout(resolve, 0));
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('dialog .cancel')!.click();
    http
      .expectOne('/api/session-runs/12/cancel')
      .flush(null, { status: 204, statusText: 'No Content' });
    expect(await second).toBe(true);
    http
      .expectOne('/api/dashboard')
      .flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0 });
    await harness.fixture.whenStable();
  });

  it('keeps the session active when the user declines to leave', async () => {
    const { harness, component } = await open(10);
    const router = TestBed.inject(Router);
    const navigation = router.navigateByUrl('/home');
    await new Promise((resolve) => setTimeout(resolve, 0));
    harness
      .routeNativeElement!.querySelector<HTMLButtonElement>('dialog .dialog-continue')!
      .click();
    expect(await navigation).toBe(false);
    expect(router.url).toBe('/sessions/12/timer');
    expect(component.state()).toBe('active');
    http.expectNone('/api/session-runs/12/cancel');
  });

  it('requests a browser confirmation for reload while active but not after completion', async () => {
    const { component } = await open(10);
    const activeEvent = new Event('beforeunload', { cancelable: true }) as BeforeUnloadEvent;
    component.onBeforeUnload(activeEvent);
    expect(activeEvent.defaultPrevented).toBe(true);
    component.state.set('completed');
    const completedEvent = new Event('beforeunload', { cancelable: true }) as BeforeUnloadEvent;
    component.onBeforeUnload(completedEvent);
    expect(completedEvent.defaultPrevented).toBe(false);
  });

  it('cancels a session on reload rather than resuming it', async () => {
    sessionStorage.setItem('tomatime.timer.12', 'active');
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/12/timer', TimerComponent);
    http
      .expectOne('/api/session-runs/12/cancel')
      .flush(null, { status: 204, statusText: 'No Content' });
    expect(component.state()).toBe('ended');
    http.expectNone('/api/session-runs/12/heartbeat');
  });
});
