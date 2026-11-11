import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { RouterTestingHarness } from '@angular/router/testing';
import { LoginComponent } from '../login-component/login-component';
import { ResetPasswordComponent } from './reset-password-component';

describe('ResetPasswordComponent', () => {
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let component: ResetPasswordComponent;
  let http: HttpTestingController;

  async function createComponent(token?: string): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(token === undefined ? {} : { token }) },
          },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  }

  afterEach(() => http?.verify());

  it('shows a link to request a new email when the token is missing', async () => {
    await createComponent();
    fixture.detectChanges();

    expect(component.invalidLink).toBe(true);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href*="forgotPassword=true"]')).toBeTruthy();
    component.submit();
    http.expectNone('/api/auth/reset-password');
  });

  describe('Password reset navigation', () => {
    it('opens the forgot-password form when requesting a new link', async () => {
      await TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
          provideRouter([
            { path: 'reset-password', component: ResetPasswordComponent },
            { path: 'login', component: LoginComponent },
          ]),
        ],
      }).compileComponents();
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/reset-password', ResetPasswordComponent);
      const link: HTMLAnchorElement = harness.routeNativeElement!.querySelector(
        'a[href*="forgotPassword=true"]',
      )!;
      link.click();
      await harness.fixture.whenStable();
      expect(harness.routeNativeElement?.querySelector('#reset-email')).toBeTruthy();
    });
  });

  it('does not submit short, blank or mismatched passwords', async () => {
    await createComponent('valid-token');

    for (const [password, confirmation] of [
      ['short', 'short'],
      ['        ', '        '],
      ['valid-password', 'different-password'],
      ['x'.repeat(129), 'x'.repeat(129)],
    ]) {
      component.form.setValue({ newPassword: password, confirmPassword: confirmation });
      component.submit();
      expect(component.form.invalid).toBe(true);
    }
    http.expectNone('/api/auth/reset-password');
  });

  it('sends the token and password once, then shows success and clears passwords', async () => {
    await createComponent('valid-token');
    component.form.setValue({ newPassword: 'new-password', confirmPassword: 'new-password' });
    component.submit();
    component.submit();

    const request = http.expectOne('/api/auth/reset-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token: 'valid-token', newPassword: 'new-password' });
    request.flush({ message: 'Password reset successfully.' });
    fixture.detectChanges();

    expect(component.isComplete).toBe(true);
    expect(component.form.getRawValue()).toEqual({ newPassword: '', confirmPassword: '' });
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/login"]')).toBeTruthy();
  });

  it('shows the request-a-new-link state for an invalid or expired token', async () => {
    await createComponent('expired-token');
    component.form.setValue({ newPassword: 'new-password', confirmPassword: 'new-password' });
    component.submit();
    http
      .expectOne('/api/auth/reset-password')
      .flush(
        { message: 'The reset token is invalid or expired.' },
        { status: 400, statusText: 'Bad Request' },
      );
    fixture.detectChanges();

    expect(component.invalidLink).toBe(true);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href*="forgotPassword=true"]')).toBeTruthy();
  });

  it('allows retrying after a connection error', async () => {
    await createComponent('valid-token');
    component.form.setValue({ newPassword: 'new-password', confirmPassword: 'new-password' });
    component.submit();
    http.expectOne('/api/auth/reset-password').error(new ProgressEvent('error'));

    expect(component.errorKey).toBe('auth.errors.connection');
    expect(component.isSubmitting).toBe(false);
    expect(component.invalidLink).toBe(false);
  });
});
