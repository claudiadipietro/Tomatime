import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

const matchingPasswordsValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  return password === confirmPassword ? null : { passwordMismatch: true };
};

@Component({
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  selector: 'app-register-component',
  styleUrl: './register-component.css',
  templateUrl: './register-component.html',
})
export class RegisterComponent {
  readonly form = new FormGroup(
    {
      name: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(2), Validators.maxLength(100)],
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email, Validators.maxLength(320)],
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(8), Validators.maxLength(128)],
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: matchingPasswordsValidator },
  );

  isSubmitting = false;
  showPassword = false;
  showConfirmPassword = false;
  errorKey: string | null = null;
  successKey: string | null = null;

  constructor(private readonly authService: AuthService) {}

  get passwordsDoNotMatch(): boolean {
    return this.form.hasError('passwordMismatch') && this.form.controls.confirmPassword.touched;
  }

  submit(): void {
    this.errorKey = null;
    this.successKey = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email, password } = this.form.getRawValue();
    this.isSubmitting = true;
    this.authService.register({ name: name.trim(), email: email.trim(), password }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successKey = 'auth.register.success';
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        this.errorKey =
          error.status === 409
            ? 'auth.errors.emailExists'
            : error.status === 0
              ? 'auth.errors.connection'
              : error.status === 400
                ? 'auth.errors.invalidRegistration'
                : 'auth.errors.generic';
      },
    });
  }
}
