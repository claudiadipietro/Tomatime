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
import { ProfileComponent } from './profile-component';

describe('Profile', () => {
  let http: HttpTestingController;
  const profile = { name: 'Jane', email: 'jane@example.com', pomoBalance: 24, currentTheme: 'pink', notificationsEnabled: true };

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(), provideRouter(routes),
      provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
    ] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); sessionStorage.clear(); });

  function signIn(): void {
    TestBed.inject(AuthSessionService).save({
      userId: 7, name: 'Jane', email: 'jane@example.com', accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
  }

  it('requires authentication', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/profile', LoginComponent);
    http.expectNone('/api/profile');
  });

  it('opens from the menu and displays the authenticated profile', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/home', MainScreenComponent);
    http.expectOne('/api/dashboard').flush({ pomoBalance: 0, nextSession: null, focusMinutes: 0, completedSessions: 0 });
    harness.routeNativeElement!.querySelector<HTMLButtonElement>('.menu-toggle')!.click();
    harness.detectChanges();
    harness.routeNativeElement!.querySelector<HTMLAnchorElement>('a[href="/profile"]')!.click();
    await harness.fixture.whenStable();
    const request = http.expectOne('/api/profile');
    expect(request.request.headers.has('Authorization')).toBe(true);
    request.flush(profile);
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/profile');
    expect(harness.routeNativeElement!.textContent).toContain('jane@example.com');
    expect(harness.routeDebugElement?.componentInstance.profile()?.pomoBalance).toBe(24);
  });

  it('updates the notification preference only after success and supports retry', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/profile', ProfileComponent);
    http.expectOne('/api/profile').flush('', { status: 500, statusText: 'Error' });
    expect(component.loadError()).toBe(true);
    component.load();
    http.expectOne('/api/profile').flush(profile);
    component.updateNotifications(false);
    http.expectOne('/api/profile/notifications').flush('', { status: 500, statusText: 'Error' });
    expect(component.profile()?.notificationsEnabled).toBe(true);
    expect(component.notificationError()).toBe(true);
    component.updateNotifications(false);
    const request = http.expectOne('/api/profile/notifications');
    expect(request.request.body).toEqual({ enabled: false });
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(component.profile()?.notificationsEnabled).toBe(false);
  });

  it('validates passwords and handles incorrect and successful changes', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl('/profile', ProfileComponent);
    http.expectOne('/api/profile').flush(profile);
    component.passwordForm.setValue({ currentPassword: 'wrong-password', newPassword: 'new-password', confirmPassword: 'different' });
    component.changePassword();
    expect(component.passwordError()).toBe('profile.passwordMismatch');
    http.expectNone('/api/profile/password');
    component.passwordForm.controls.confirmPassword.setValue('new-password');
    component.changePassword();
    http.expectOne('/api/profile/password').flush('', { status: 400, statusText: 'Bad Request' });
    expect(component.passwordError()).toBe('profile.wrongPassword');
    component.changePassword();
    http.expectOne('/api/profile/password').flush(null, { status: 204, statusText: 'No Content' });
    expect(component.passwordSuccess()).toBe(true);
    expect(component.passwordForm.value.currentPassword).toBe('');
  });
});
