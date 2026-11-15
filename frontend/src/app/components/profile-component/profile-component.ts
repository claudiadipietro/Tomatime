import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { AuthSessionService } from '../../services/auth-session.service';
import { Profile, ProfileService } from '../../services/profile.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';

@Component({
  selector: 'app-profile',
  imports: [MainHeaderComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './profile-component.html',
  styleUrl: './profile-component.css',
})
export class ProfileComponent {
  private readonly service = inject(ProfileService);
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly profile = signal<Profile | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly notificationsOpen = signal(false);
  readonly notificationBusy = signal(false);
  readonly notificationError = signal(false);
  readonly passwordOpen = signal(false);
  readonly passwordBusy = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordSuccess = signal(false);

  readonly passwordForm = new FormGroup({
    currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(128)] }),
    newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8), Validators.maxLength(128), Validators.pattern(/\S/)] }),
    confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.service.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile) => { this.profile.set(profile); this.loading.set(false); },
      error: () => { this.loading.set(false); this.loadError.set(true); },
    });
  }

  updateNotifications(enabled: boolean): void {
    if (this.notificationBusy() || !this.profile()) return;
    this.notificationBusy.set(true);
    this.notificationError.set(false);
    this.service.updateNotifications(enabled)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.notificationBusy.set(false)))
      .subscribe({
        next: () => this.profile.update((profile) => profile ? { ...profile, notificationsEnabled: enabled } : null),
        error: () => this.notificationError.set(true),
      });
  }

  changePassword(): void {
    if (this.passwordBusy()) return;
    this.passwordError.set(null);
    this.passwordSuccess.set(false);
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();
    if (this.passwordForm.invalid || newPassword !== confirmPassword) {
      this.passwordForm.markAllAsTouched();
      this.passwordError.set(newPassword !== confirmPassword ? 'profile.passwordMismatch' : 'profile.passwordInvalid');
      return;
    }
    this.passwordBusy.set(true);
    this.service.changePassword(currentPassword, newPassword)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.passwordBusy.set(false)))
      .subscribe({
        next: () => { this.passwordForm.reset(); this.passwordSuccess.set(true); },
        error: (error: HttpErrorResponse) =>
          this.passwordError.set(error.status === 400 ? 'profile.wrongPassword' : 'profile.passwordError'),
      });
  }

  async logout(): Promise<void> {
    if (await this.router.navigate(['/login'])) this.auth.clear();
  }
}
