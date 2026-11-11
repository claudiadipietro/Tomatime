import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

@Component({
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  selector: 'app-login-component',
  styleUrl: './login-component.css',
  templateUrl: './login-component.html',
})
export class LoginComponent {
  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(320)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(128)],
    }),
  });

  readonly forgotPasswordForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(320)],
    }),
  });

  isSubmitting = false;
  isRequestingReset = false;
  isForgotPasswordMode: boolean;
  showPassword = false;
  errorKey: string | null = null;
  successKey: string | null = null;

  constructor(
    private readonly authService: AuthService,
    private readonly changeDetector: ChangeDetectorRef,
    private readonly router: Router,
    route: ActivatedRoute,
  ) {
    this.isForgotPasswordMode = route.snapshot.queryParamMap.get('forgotPassword') === 'true';
  }

  submit(): void {
    this.errorKey = null;
    this.successKey = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.isSubmitting = false;
        void this.router.navigate(['/home']);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        this.errorKey =
          error.status === 401
            ? 'auth.errors.invalidCredentials'
            : error.status === 0
              ? 'auth.errors.connection'
              : 'auth.errors.generic';
        this.changeDetector.markForCheck();
      },
    });
  }

  requestPasswordReset(): void {
    this.errorKey = null;
    this.successKey = null;

    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    this.isRequestingReset = true;
    this.authService.requestPasswordReset(this.forgotPasswordForm.getRawValue()).subscribe({
      next: () => {
        this.isRequestingReset = false;
        this.successKey = 'auth.forgotPassword.success';
        this.changeDetector.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        this.isRequestingReset = false;
        this.errorKey = error.status === 0 ? 'auth.errors.connection' : 'auth.errors.generic';
        this.changeDetector.markForCheck();
      },
    });
  }

  toggleForgotPassword(): void {
    this.isForgotPasswordMode = !this.isForgotPasswordMode;
    this.errorKey = null;
    this.successKey = null;
  }
}
