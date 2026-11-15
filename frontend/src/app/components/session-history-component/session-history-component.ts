import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../i18n/language.service';
import { SessionHistoryItem } from '../../models/session-history.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionHistoryService } from '../../services/session-history.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';

interface HistoryGroup {
  key: string;
  date: Date | null;
  sessions: SessionHistoryItem[];
}

@Component({
  selector: 'app-session-history',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  templateUrl: './session-history-component.html',
  styleUrl: './session-history-component.css',
})
export class SessionHistoryComponent {
  private readonly history = inject(SessionHistoryService);
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly language = inject(LanguageService);
  readonly sessions = signal<SessionHistoryItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly groups = computed(() => {
    const groups = new Map<string, HistoryGroup>();
    for (const session of this.sessions()) {
      const date = session.actualStartTime ? new Date(session.actualStartTime) : null;
      const key = date ? `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}` : 'unknown';
      if (!groups.has(key)) groups.set(key, { key, date, sessions: [] });
      groups.get(key)!.sessions.push(session);
    }
    return Array.from(groups.values());
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.history
      .getHistory()
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
          } else {
            this.error.set(true);
          }
        },
      });
  }

  isToday(date: Date | null): boolean {
    if (!date) return false;
    const today = new Date();
    return (
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate()
    );
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      dateStyle: 'long',
    }).format(date);
  }

  formatTime(value: string): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      hour: 'numeric',
      minute: '2-digit',
    }).format(new Date(value));
  }
}
