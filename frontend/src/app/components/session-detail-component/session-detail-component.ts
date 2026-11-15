import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { LanguageService } from '../../i18n/language.service';
import { SessionHistoryItem } from '../../models/session-history.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionHistoryService } from '../../services/session-history.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';

@Component({
  selector: 'app-session-detail',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  templateUrl: './session-detail-component.html',
  styleUrl: './session-detail-component.css',
})
export class SessionDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly history = inject(SessionHistoryService);
  private readonly auth = inject(AuthSessionService);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;

  readonly session = signal<SessionHistoryItem | null>(null);
  readonly loading = signal(true);
  readonly error = signal<'notFound' | 'error' | null>(null);
  readonly fromCalendar = this.route.snapshot.queryParamMap.get('from') === 'calendar';
  readonly calendarDate = this.route.snapshot.queryParamMap.get('date');

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
  }

  load(): void {
    this.request?.unsubscribe();
    const rawId = this.route.snapshot.paramMap.get('id');
    const id = Number(rawId);
    this.session.set(null);
    this.error.set(null);
    if (!rawId || !/^[1-9]\d*$/.test(rawId) || !Number.isSafeInteger(id) || id > 2147483647) {
      this.loading.set(false);
      this.error.set('notFound');
      return;
    }
    this.loading.set(true);
    this.request = this.history
      .getItem(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.session.set(session);
          this.loading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else {
            this.error.set(error.status === 404 ? 'notFound' : 'error');
          }
        },
      });
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      dateStyle: 'long',
    }).format(new Date(value));
  }

  formatTime(value: string): string {
    return new Intl.DateTimeFormat(this.language.currentLanguage() || 'en', {
      hour: 'numeric',
      minute: '2-digit',
    }).format(new Date(value));
  }

  isDifferentDay(start: string, end: string): boolean {
    const first = new Date(start);
    const last = new Date(end);
    return (
      first.getFullYear() !== last.getFullYear() ||
      first.getMonth() !== last.getMonth() ||
      first.getDate() !== last.getDate()
    );
  }
}
