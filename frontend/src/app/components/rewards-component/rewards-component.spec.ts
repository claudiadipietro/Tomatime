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
    http.expectOne('/api/reward-shop').flush(base);
    component.purchase('pink');
    const purchase = http.expectOne('/api/reward-shop/pink/purchase');
    expect(purchase.request.body.operationId).toBeTruthy();
    purchase.flush({ ...base, pomoBalance: 5, pinkUnlocked: true });
    expect(component.owned('pink')).toBe(true);
    component.activate('pink');
    http.expectOne('/api/reward-shop/themes/pink/activate')
      .flush({ ...base, pomoBalance: 5, pinkUnlocked: true, currentTheme: 'pink' });
    expect(document.documentElement.dataset['theme']).toBe('pink');
  });

  it('allows retrying a failed load', async () => {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/rewards', RewardsComponent);
    http.expectOne('/api/reward-shop').flush('', { status: 500, statusText: 'Error' });
    expect(component.error()).toBe('rewards.loadError');
    component.load();
    http.expectOne('/api/reward-shop').flush({
      pomoBalance: 0, pendingExtraBreaks: 1, currentTheme: 'default', pinkUnlocked: false, blueUnlocked: false,
    });
    expect(component.state()?.pendingExtraBreaks).toBe(1);
    expect(component.error()).toBeNull();
  });
});
