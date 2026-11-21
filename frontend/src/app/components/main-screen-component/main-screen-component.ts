import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../i18n/language.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';
import { Dashboard } from '../../models/dashboard.model';
import { DashboardService } from '../../services/dashboard.service';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionService } from '../../services/session.service';
import { pipe } from 'rxjs/internal/util/pipe';

@Component({
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  selector: 'app-main-screen-component',
  templateUrl: './main-screen-component.html',
  styleUrl: './main-screen-component.css',
})
export class MainScreenComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly sessionService = inject(SessionService);
  private readonly session = inject(AuthSessionService);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly dashboard = signal<Dashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly starting = signal(false);
  readonly startError = signal(false);

  readonly nextSessionDate = computed(() => {
    const date = this.dashboard()?.nextSession?.plannedStartTime;
    if (!date) return '';
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(date));
  });

  readonly focusTime = computed(() => {
    const minutes = this.dashboard()?.focusMinutes ?? 0;
    const hours = Math.floor(minutes / 60);
    const remainder = minutes % 60;
    return hours > 0 ? `${hours} h${remainder > 0 ? ` ${remainder} min` : ''}` : `${minutes} min`;
  });

  formatSessionDate(date: string): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(date));
  }

  constructor() {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(false);
    this.dashboardService
      .getDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dashboard) => {
          this.dashboard.set(dashboard);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          if (error.status === 401) {
            this.logout();
          } else {
            this.error.set(true);
          }
        },
      });
  }

  startPlanned(sessionId: number | undefined): void {
    if (sessionId === undefined || this.starting()) return;
    this.starting.set(true);
    this.startError.set(false);
    this.sessionService
      .startPlanned(sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.starting.set(false);
          void this.router.navigate(['/sessions', sessionId, 'timer']);
        },
        error: (error: HttpErrorResponse) => {
          this.starting.set(false);
          if (error.status === 401) this.logout();
          else this.startError.set(true);
        },
      });
  }

  logout(): void {
    this.session.clear();
    void this.router.navigate(['/login']);
  }

  cancelPlanned(sessionId: number | undefined): void {
    if (sessionId === undefined) return;
    this.sessionService
      .cancel(sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loadDashboard();
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) this.logout();
        },
      });
  }
}
