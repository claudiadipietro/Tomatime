import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { LoginComponent } from './login-component';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows the request confirmation after the server responds', async () => {
    component.toggleForgotPassword();
    component.forgotPasswordForm.setValue({ email: 'jane@example.com' });
    component.requestPasswordReset();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/forgot-password')
      .flush({ message: 'If the email exists, we will try to send password reset instructions.' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();
  });
});
