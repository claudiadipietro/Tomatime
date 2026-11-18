import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../../services/auth-session.service';
import { authInterceptor } from '../../services/auth.interceptor';
import { RewardsComponent } from './rewards-component';

describe('Rewards', () => {
  let http: HttpTestingController;
  const catalog = [
    { id: -1, name: 'pink', cost: 7, type: 0 },
    { id: -2, name: 'blue', cost: 5, type: 0 },
    { id: -3, name: 'extra-break', cost: 3, type: 1 },
  ];

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
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
      userId: 7, name: 'Jane', email: 'jane@example.com', accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
    localStorage.clear();
    delete document.documentElement.dataset['theme'];
  });

  it('purchases and activates a theme', async () => {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/rewards', RewardsComponent);
    const base = { pomoBalance: 10, pendingExtraBreaks: 0, currentTheme: 'default', pinkUnlocked: false, blueUnlocked: false };
    http.expectOne('/api/users/rewards').flush(base);
    http.expectOne('/api/rewards').flush(catalog);
    component.purchase('pink');
    const purchase = http.expectOne('/api/rewards/-1/redeem');
    expect(purchase.request.body.operationId).toBeTruthy();
    purchase.flush({ ...base, pomoBalance: 3, pinkUnlocked: true });
    expect(component.owned('pink')).toBe(true);
    component.activate('pink');
    const activate = http.expectOne('/api/users/theme');
    expect(activate.request.method).toBe('PUT');
    expect(activate.request.body).toEqual({ theme: 'pink' });
    activate.flush({ ...base, pomoBalance: 3, pinkUnlocked: true, currentTheme: 'pink' });
    expect(document.documentElement.dataset['theme']).toBe('pink');
  });

  it('allows retrying a failed load', async () => {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/rewards', RewardsComponent);
    const catalogRequest = http.expectOne('/api/rewards');
    http.expectOne('/api/users/rewards').flush('', { status: 500, statusText: 'Error' });
    expect(catalogRequest.cancelled).toBe(true);
    expect(component.error()).toBe('rewards.loadError');
    component.load();
    http.expectOne('/api/users/rewards').flush({
      pomoBalance: 0, pendingExtraBreaks: 1, currentTheme: 'default', pinkUnlocked: false, blueUnlocked: false,
    });
    http.expectOne('/api/rewards').flush(catalog);
    expect(component.state()?.pendingExtraBreaks).toBe(1);
    expect(component.error()).toBeNull();
  });

  it('uses the catalog cost to enable purchases', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/rewards', RewardsComponent);
    http.expectOne('/api/users/rewards').flush({
      pomoBalance: 4, pendingExtraBreaks: 0, currentTheme: 'default', pinkUnlocked: false, blueUnlocked: false,
    });
    http.expectOne('/api/rewards').flush(catalog);
    harness.detectChanges();

    const breakButton = harness.routeNativeElement!.querySelector('.break-card button') as HTMLButtonElement;
    expect(breakButton.disabled).toBe(false);
    const buttons = harness.routeNativeElement!.querySelectorAll('.theme-card button');
    expect((buttons[1] as HTMLButtonElement).disabled).toBe(true);
  });

  it('reports an incomplete catalog instead of displaying fixed prices', async () => {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/rewards', RewardsComponent);
    http.expectOne('/api/users/rewards').flush({
      pomoBalance: 10, pendingExtraBreaks: 0, currentTheme: 'default', pinkUnlocked: false, blueUnlocked: false,
    });
    http.expectOne('/api/rewards').flush(catalog.slice(0, 2));
    expect(component.state()).toBeNull();
    expect(component.error()).toBe('rewards.loadError');
  });
});
