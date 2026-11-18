import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { Profile, ProfileService } from '../../services/profile.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';
import { PushNotificationsService } from '../../services/push-notifications.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-profile',
  imports: [MainHeaderComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './profile-component.html',
  styleUrl: './profile-component.css',
})
export class ProfileComponent {
  private readonly service = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly push = inject(PushNotificationsService);

  readonly profile = signal<Profile | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly notificationsOpen = signal(false);
  readonly notificationBusy = signal(false);
  readonly notificationError = signal<string | null>(null);
  readonly deviceSubscribed = signal(false);
  readonly pushSupported = this.push.supported;
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
    if (inject(ActivatedRoute).snapshot.queryParamMap.has('logoutError'))
      this.notificationError.set('profile.logoutError');
    this.load();
    void this.refreshDevice();
  }

  private async refreshDevice(): Promise<void> {
    try { this.deviceSubscribed.set(await this.push.registered()); }
    catch { this.notificationError.set('profile.pushError'); }
  }
  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.service.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile) => { this.profile.set(profile); this.loading.set(false); },
      error: () => { this.loading.set(false); this.loadError.set(true); },
    });
  }

  async updateNotifications(enabled: boolean): Promise<void> {
    if (this.notificationBusy() || !this.profile()) return;
    this.notificationBusy.set(true);
    this.notificationError.set(null);
    try {
      if (enabled) {
        if (!this.pushSupported) { this.notificationError.set('profile.pushUnsupported'); return; }
        await this.push.subscribe();
        this.deviceSubscribed.set(true);
      }
      await firstValueFrom(this.service.updateNotifications(enabled));
      this.profile.update((profile) => profile ? { ...profile, notificationsEnabled: enabled } : null);
    } catch (error) {
      this.notificationError.set(error instanceof DOMException && error.name === 'NotAllowedError'
        ? 'profile.pushDenied'
        : error instanceof HttpErrorResponse && error.status === 503
          ? 'profile.pushNotConfigured' : 'profile.notificationError');
    } finally { this.notificationBusy.set(false); }
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
    this.notificationError.set(null);
    try {
      try {
        await this.push.unsubscribe();
      } catch {
        // Continue logging out if push subscription cleanup fails.
      }
      await firstValueFrom(this.auth.logout());
      await this.router.navigate(['/login']);
    } catch {
      this.notificationError.set('profile.logoutError');
    }
  }
}
