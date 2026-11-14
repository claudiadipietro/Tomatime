import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionService } from '../../services/session.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';
import { SessionDurationComponent } from '../session-duration-component/session-duration-component';

@Component({
  selector: 'app-new-session',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent, SessionDurationComponent],
  templateUrl: './new-session-component.html',
  styleUrl: './new-session-component.css',
})
export class NewSessionComponent {
  private readonly sessions = inject(SessionService);
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly duration = signal(50);
  readonly blocks = computed(() => Array.from({ length: this.duration() / 25 }, (_, i) => i));
  readonly submitting = signal(false);
  readonly started = signal(false);
  readonly error = signal(false);

  changeDuration(delta: number): void {
    this.duration.update((duration) => Math.min(300, Math.max(25, duration + delta)));
  }

  start(): void {
    if (this.submitting() || this.started()) return;
    this.error.set(false);
    this.submitting.set(true);
    this.sessions
      .startSpontaneous(this.duration())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.started.set(true);
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else {
            this.error.set(true);
          }
        },
      });
  }
}
