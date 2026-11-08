import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { RegisterComponent } from './register-component';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('requires matching passwords before registration', () => {
    component.form.setValue({
      name: 'Jane Doe',
      email: 'jane@example.com',
      password: 'secure-password',
      confirmPassword: 'different-password',
    });

    expect(component.form.invalid).toBe(true);
    expect(component.passwordsDoNotMatch).toBe(false);

    component.form.controls.confirmPassword.markAsTouched();

    expect(component.passwordsDoNotMatch).toBe(true);
  });
});
