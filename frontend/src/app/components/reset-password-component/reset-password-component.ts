import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

const matchingPasswordsValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null =>
  control.get('newPassword')?.value === control.get('confirmPassword')?.value
    ? null
    : { passwordMismatch: true };

@Component({
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  selector: 'app-reset-password-component',
  styleUrl: './reset-password-component.css',
  templateUrl: './reset-password-component.html',
})
export class ResetPasswordComponent {
  readonly form = new FormGroup(
    {
      newPassword: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(128),
          Validators.pattern(/\S/),
        ],
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: matchingPasswordsValidator },
  );

  private readonly token: string | null;
  invalidLink: boolean;
  isSubmitting = false;
  isComplete = false;
  showPassword = false;
  showConfirmPassword = false;
  errorKey: string | null = null;

  constructor(
    private readonly authService: AuthService,
    private readonly changeDetector: ChangeDetectorRef,
    route: ActivatedRoute,
  ) {
    this.token = route.snapshot.queryParamMap.get('token');
    this.invalidLink = !this.token?.trim();
  }

  get passwordsDoNotMatch(): boolean {
    return this.form.hasError('passwordMismatch') && this.form.controls.confirmPassword.touched;
  }

  submit(): void {
    if (this.isSubmitting || this.isComplete || this.invalidLink || !this.token) {
      return;
    }

    this.errorKey = null;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.authService
      .resetPassword({ token: this.token, newPassword: this.form.getRawValue().newPassword })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.isComplete = true;
          this.form.reset();
          this.changeDetector.markForCheck();
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting = false;
          if (error.status === 400) {
            this.invalidLink = true;
            this.form.reset();
          } else {
            this.errorKey = error.status === 0 ? 'auth.errors.connection' : 'auth.errors.generic';
          }
          this.changeDetector.markForCheck();
        },
      });
  }
}
