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
import { vi } from 'vitest';

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
    http.expectOne('/api/sessions/12/heartbeat').flush({
      id: 12,
      duration: 50,
      actualStartTime: new Date(now - elapsedSeconds * 1000).toISOString(),
      serverTime: new Date(now).toISOString(),
      breakExtensions: [],
      pomoBalance: 4,
      extraBreakCost: 7,
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

  it('advances to a server-confirmed break and can buy an extra break', async () => {
    const { harness, component } = await open(10);
    expect(component.testToolsEnabled).toBe(true);
    component.advanceTestTime(true);
    component.advanceTestTime(true);
    const advance = http.expectOne('/api/testing/sessions/12/advance');
    expect(advance.request.body).toEqual({ seconds: 1490 });
    const now = Date.now();
    const active = {
      id: 12, duration: 50, actualStartTime: new Date(now - 10_000).toISOString(),
      serverTime: new Date(now + 1490_000).toISOString(),
      breakExtensions: [], pomoBalance: 10, extraBreakCost: 3,
    };
    advance.flush(active);
    expect(component.isBreak()).toBe(true);
    expect(component.remaining()).toBe('05:00');
    component.extendBreak();
    http.expectOne('/api/sessions/12/extra-break').flush({
      pomoBalance: 7, pendingBreaks: 0, currentTheme: 'default', ownsPink: false, ownsBlue: false,
    });
    harness.detectChanges();
    expect((harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement).disabled).toBe(true);
    component.extendBreak();
    http.expectNone('/api/sessions/12/extra-break');
    http.expectOne('/api/sessions/12/heartbeat').flush({
      ...active, breakExtensions: [1], pomoBalance: 7,
    });
    expect(component.remaining()).toBe('10:00');
    harness.detectChanges();
    expect(component.canExtendBreak()).toBe(false);
    expect((harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement).disabled).toBe(true);
    component.extendBreak();
    http.expectNone('/api/sessions/12/extra-break');
  });

  it('reports advancement failure without moving the timer locally', async () => {
    const { component } = await open(10);
    component.advanceTestTime();
    http.expectOne('/api/testing/sessions/12/advance')
      .flush('', { status: 404, statusText: 'Not Found' });
    expect(component.isBreak()).toBe(false);
    expect(component.advancing()).toBe(false);
    expect(component.error()).toBe('timer.testAdvanceError');
  });

  it('keeps the purchase button disabled while an uncertain purchase is confirmed separately', async () => {
    const { harness, component } = await open(10);
    component.advanceTestTime(true);
    const now = Date.now();
    const active = {
      id: 12, duration: 50, actualStartTime: new Date(now - 1500_000).toISOString(),
      serverTime: new Date(now).toISOString(), breakExtensions: [], pomoBalance: 10, extraBreakCost: 3,
    };
    http.expectOne('/api/testing/sessions/12/advance').flush(active);
    component.extendBreak();
    const purchase = http.expectOne('/api/sessions/12/extra-break');
    const operationId = purchase.request.body.operationId;
    purchase.flush('', { status: 500, statusText: 'Error' });
    harness.detectChanges();
    const button = harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    button.click();
    component.extendBreak();
    http.expectNone('/api/sessions/12/extra-break');
    component.confirmPendingExtension();
    const retry = http.expectOne('/api/sessions/12/extra-break');
    expect(retry.request.body.operationId).toBe(operationId);
    retry.flush({ pomoBalance: 7 });
    http.expectOne('/api/sessions/12/heartbeat').flush({
      ...active, breakExtensions: [1], pomoBalance: 7,
    });
    harness.detectChanges();
    expect(button.disabled).toBe(true);
    expect(component.pendingExtension).toBe(false);
  });

  it('uses the server catalog price to enable an extra break', async () => {
    const { harness, component } = await open(1500);
    harness.detectChanges();
    const button = harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    component.extendBreak();
    http.expectNone('/api/sessions/12/extra-break');

    const hidden = vi.spyOn(document, 'hidden', 'get');
    try {
      hidden.mockReturnValue(false);
      document.dispatchEvent(new Event('visibilitychange'));
      const now = Date.now();
      http.expectOne('/api/sessions/12/heartbeat').flush({
        id: 12, duration: 50, actualStartTime: new Date(now - 1500_000).toISOString(),
        serverTime: new Date(now).toISOString(), breakExtensions: [], pomoBalance: 4, extraBreakCost: 3,
      });
      harness.detectChanges();
      expect(button.disabled).toBe(false);
    } finally {
      hidden.mockRestore();
    }
  });

  it('blocks a stored extra in the first break but allows one in the next break', async () => {
    const { harness, component } = await open(1500);
    const hidden = vi.spyOn(document, 'hidden', 'get');
    try {
      hidden.mockReturnValue(false);
      const now = Date.now();
      const active = {
        id: 12, duration: 75, actualStartTime: new Date(now - 1500_000).toISOString(),
        serverTime: new Date(now).toISOString(), breakExtensions: [1, 0], pomoBalance: 20, extraBreakCost: 3,
      };
      document.dispatchEvent(new Event('visibilitychange'));
      http.expectOne('/api/sessions/12/heartbeat').flush(active);
      harness.detectChanges();
      expect(component.remaining()).toBe('10:00');
      expect(component.canExtendBreak()).toBe(false);
      expect((harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement).disabled).toBe(true);
      component.extendBreak();
      http.expectNone('/api/sessions/12/extra-break');

      document.dispatchEvent(new Event('visibilitychange'));
      http.expectOne('/api/sessions/12/heartbeat').flush({
        ...active, serverTime: new Date(now + 2100_000).toISOString(),
      });
      harness.detectChanges();
      expect(component.block()).toBe(2);
      expect(component.isBreak()).toBe(true);
      expect(component.canExtendBreak()).toBe(true);
      expect((harness.routeNativeElement!.querySelector('button.primary') as HTMLButtonElement).disabled).toBe(false);
    } finally {
      hidden.mockRestore();
    }
  });

  it('completes only after the focus blocks and break and displays the awarded balance', async () => {
    const { harness, component } = await open(3300);
    const request = http.expectOne('/api/sessions/12/complete');
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
  });

  it('keeps the session active when navigating home', async () => {
    const { harness, component } = await open(10);
    const router = TestBed.inject(Router);
    expect(await router.navigateByUrl('/home')).toBe(true);
    expect(component.state()).toBe('active');
    http.expectNone('/api/sessions/12/cancel');
    http
      .expectOne('/api/dashboard')
      .flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0, activeSessions: [] });
    await harness.fixture.whenStable();
  });

  it('only cancels on confirmation and allows retry after an error', async () => {
    const { harness, component } = await open(10);
    component.confirmCancellation();
    component.decideLeave(false);
    http.expectNone('/api/sessions/12/cancel');
    component.confirmCancellation();
    component.decideLeave(true);
    http.expectOne('/api/sessions/12/cancel').flush('', { status: 500, statusText: 'Error' });
    expect(component.error()).toBe('timer.cancelError');
    expect(component.state()).toBe('active');
    component.confirmCancellation();
    component.decideLeave(true);
    http.expectOne('/api/sessions/12/cancel').flush(null, { status: 204, statusText: 'No Content' });
    expect(component.state()).toBe('ended');
    await harness.fixture.whenStable();
  });

  it('resumes a previously opened session on reload', async () => {
    sessionStorage.setItem('tomatime.timer.12', 'active');
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/sessions/12/timer', TimerComponent);
    const now = Date.now();
    http.expectOne('/api/sessions/12/heartbeat').flush({
      id: 12, duration: 25, actualStartTime: new Date(now - 180_000).toISOString(),
      serverTime: new Date(now).toISOString(), breakExtensions: [], pomoBalance: 0,
    });
    expect(component.state()).toBe('active');
    expect(component.remaining()).toBe('22:00');
    http.expectNone('/api/sessions/12/cancel');
  });

  it('resynchronizes after the screen is unlocked and completes if time elapsed', async () => {
    const { component } = await open(10);
    const hidden = vi.spyOn(document, 'hidden', 'get');
    try {
      hidden.mockReturnValue(true);
      document.dispatchEvent(new Event('visibilitychange'));
      window.dispatchEvent(new Event('pagehide'));
      http.expectNone('/api/sessions/12/cancel');
      hidden.mockReturnValue(false);
      document.dispatchEvent(new Event('visibilitychange'));
      const now = Date.now();
      http.expectOne('/api/sessions/12/heartbeat').flush({
        id: 12, duration: 50, actualStartTime: new Date(now - 3300_000).toISOString(),
        serverTime: new Date(now).toISOString(), breakExtensions: [], pomoBalance: 0,
      });
      http.expectOne('/api/sessions/12/complete').flush({ pomosEarned: 4 });
      expect(component.state()).toBe('completed');
    } finally {
      hidden.mockRestore();
    }
  });

  it('ignores a stale heartbeat that returns after the screen is unlocked', async () => {
    const { component } = await open(10);
    const hidden = vi.spyOn(document, 'hidden', 'get');
    try {
      hidden.mockReturnValue(false);
      document.dispatchEvent(new Event('visibilitychange'));
      const stale = http.expectOne('/api/sessions/12/heartbeat');
      hidden.mockReturnValue(true);
      document.dispatchEvent(new Event('visibilitychange'));
      hidden.mockReturnValue(false);
      document.dispatchEvent(new Event('visibilitychange'));
      const fresh = http.expectOne('/api/sessions/12/heartbeat');
      const now = Date.now();
      const started = new Date(now - 600_000).toISOString();
      fresh.flush({ id: 12, duration: 50, actualStartTime: started, serverTime: new Date(now).toISOString() });
      stale.flush({ id: 12, duration: 50, actualStartTime: started, serverTime: new Date(now + 1000).toISOString() });
      expect(component.elapsed()).toBe(600);
    } finally {
      hidden.mockRestore();
    }
  });
});
