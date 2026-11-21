import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { interval, Subscription } from 'rxjs';
import { LanguageService } from '../../i18n/language.service';
import { CalendarSession } from '../../models/calendar-session.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionCalendarService } from '../../services/session-calendar.service';
import { SessionService } from '../../services/session.service';
import { localDateKey } from '../../utils/local-date';
import { MainHeaderComponent } from '../main-header-component/main-header-component';

interface CalendarDay {
  date: Date;
  key: string;
  inMonth: boolean;
  hasSessions: boolean;
}

function parseLocalDate(value: string | null): Date | null {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const [year, month, day] = value.split('-').map(Number);
  if (year < 100 || year > 9998) return null;
  const date = new Date(year, month - 1, day);
  return localDateKey(date) === value ? date : null;
}

@Component({
  selector: 'app-session-calendar',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  templateUrl: './session-calendar-component.html',
  styleUrl: './session-calendar-component.css',
})
export class SessionCalendarComponent {
  private readonly calendar = inject(SessionCalendarService);
  private readonly sessionsService = inject(SessionService);
  private readonly auth = inject(AuthSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;
  private readonly initial =
    parseLocalDate(this.route.snapshot.queryParamMap.get('date')) ?? new Date();
  readonly selected = signal(
    new Date(this.initial.getFullYear(), this.initial.getMonth(), this.initial.getDate()),
  );
  readonly month = signal(new Date(this.initial.getFullYear(), this.initial.getMonth(), 1));
  readonly sessions = signal<CalendarSession[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly starting = signal<number | null>(null);
  readonly startError = signal(false);
  readonly today = signal(localDateKey(new Date()));
  readonly selectedKey = computed(() => localDateKey(this.selected()));
  readonly selectedDateIsPast = computed(() => this.selectedKey() < this.today());
  readonly monthLabel = computed(() =>
    new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      month: 'long',
      year: 'numeric',
    }).format(this.month()),
  );
  readonly selectedLabel = computed(() =>
    new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', { dateStyle: 'long' }).format(
      this.selected(),
    ),
  );
  readonly weekdays = computed(() => {
    const format = new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      weekday: 'short',
    });
    return Array.from({ length: 7 }, (_, index) => format.format(new Date(2024, 0, index + 1)));
  });
  readonly days = computed(() => {
    const month = this.month();
    const start = new Date(month.getFullYear(), month.getMonth(), 1);
    start.setDate(start.getDate() - ((start.getDay() + 6) % 7));
    const keys = new Set(this.sessions().map((session) => localDateKey(this.sessionDate(session))));
    return Array.from({ length: 42 }, (_, index): CalendarDay => {
      const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
      const key = localDateKey(date);
      return {
        date,
        key,
        inMonth: date.getMonth() === month.getMonth(),
        hasSessions: keys.has(key),
      };
    });
  });
  readonly selectedSessions = computed(() =>
    this.sessions().filter(
      (session) => localDateKey(this.sessionDate(session)) === this.selectedKey(),
    ),
  );

  constructor() {
    this.load();
    interval(30_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshToday());
  }

  @HostListener('document:visibilitychange')
  @HostListener('window:focus')
  refreshToday(): void {
    this.today.set(localDateKey(new Date()));
  }

  planSelectedDate(): void {
    this.refreshToday();
    if (this.selectedDateIsPast()) return;
    void this.router.navigate(['/sessions/plan'], {
      queryParams: { date: this.selectedKey(), from: 'calendar' },
    });
  }

  load(): void {
    this.request?.unsubscribe();
    const month = this.month();
    const from = new Date(month.getFullYear(), month.getMonth(), 1);
    const to = new Date(month.getFullYear(), month.getMonth() + 1, 1);
    this.loading.set(true);
    this.error.set(false);
    this.sessions.set([]);
    this.request = this.calendar
      .getSessions(from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (sessions) => {
          this.sessions.set(sessions);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else this.error.set(true);
        },
      });
  }

  changeMonth(delta: number): void {
    const month = this.month();
    const next = new Date(month.getFullYear(), month.getMonth() + delta, 1);
    if (next.getFullYear() < 100 || next.getFullYear() > 9998) return;
    this.month.set(next);
    this.selected.set(next);
    this.load();
  }

  select(day: CalendarDay): void {
    if (!day.inMonth) {
      this.month.set(new Date(day.date.getFullYear(), day.date.getMonth(), 1));
      this.selected.set(day.date);
      this.load();
    } else this.selected.set(day.date);
  }

  sessionDate(session: CalendarSession): Date {
    return new Date(
      session.status === 'Planned'
        ? session.plannedStartTime
        : (session.actualStartTime ?? session.plannedStartTime),
    );
  }

  formatTime(session: CalendarSession): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      hour: 'numeric',
      minute: '2-digit',
    }).format(this.sessionDate(session));
  }

  dateLabel(day: CalendarDay): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      dateStyle: 'full',
    }).format(day.date);
  }

  start(session: CalendarSession): void {
    if (this.starting() !== null) return;
    this.starting.set(session.id);
    this.startError.set(false);
    this.sessionsService
      .startPlanned(session.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.starting.set(null);
          void this.router.navigate(['/sessions', session.id, 'timer']);
        },
        error: (error: HttpErrorResponse) => {
          this.starting.set(null);
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else {
            this.startError.set(true);
            if (error.status === 404) this.load();
          }
        },
      });
  }

  cancelPlanned(sessionId: number): void {
    this.sessionsService
      .cancel(sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.load();
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else if (error.status === 404) {
            this.load();
          }
        },
      });
  }
}
