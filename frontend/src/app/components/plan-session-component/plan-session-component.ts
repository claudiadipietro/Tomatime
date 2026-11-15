import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { interval } from 'rxjs';
import { AuthSessionService } from '../../services/auth-session.service';
import { SessionService } from '../../services/session.service';
import { localDateKey } from '../../utils/local-date';
import { MainHeaderComponent } from '../main-header-component/main-header-component';
import { SessionDurationComponent } from '../session-duration-component/session-duration-component';

@Component({
  selector: 'app-plan-session',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslatePipe,
    MainHeaderComponent,
    SessionDurationComponent,
  ],
  templateUrl: './plan-session-component.html',
  styleUrls: ['../new-session-component/new-session-component.css', './plan-session-component.css'],
})
export class PlanSessionComponent {
  private readonly sessions = inject(SessionService);
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly duration = signal(50);
  readonly submitting = signal(false);
  readonly planned = signal(false);
  readonly errorKey = signal<string | null>(null);
  readonly today = signal(localDateKey(new Date()));
  readonly form = new FormGroup({
    date: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    time: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly fromCalendar = this.route.snapshot.queryParamMap.get('from') === 'calendar';
  readonly calendarDate = this.route.snapshot.queryParamMap.get('date');

  constructor() {
    if (this.fromCalendar && this.calendarDate && /^\d{4}-\d{2}-\d{2}$/.test(this.calendarDate)) {
      this.form.controls.date.setValue(this.calendarDate);
    }
    interval(30_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshToday());
  }

  @HostListener('document:visibilitychange')
  @HostListener('window:focus')
  refreshToday(): void {
    this.today.set(localDateKey(new Date()));
  }

  submit(): void {
    if (this.submitting() || this.planned()) return;
    this.errorKey.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { date, time } = this.form.getRawValue();
    const parsed = new Date(`${date}T${time}`);
    const localDate = Number.isFinite(parsed.getTime())
      ? `${parsed.getFullYear().toString().padStart(4, '0')}-${(parsed.getMonth() + 1).toString().padStart(2, '0')}-${parsed.getDate().toString().padStart(2, '0')}`
      : '';
    const localTime = Number.isFinite(parsed.getTime())
      ? `${parsed.getHours().toString().padStart(2, '0')}:${parsed.getMinutes().toString().padStart(2, '0')}`
      : '';
    if (localDate !== date || localTime !== time || parsed.getTime() <= Date.now()) {
      this.errorKey.set('planning.invalidTime');
      return;
    }

    this.submitting.set(true);
    this.sessions
      .plan(this.duration(), parsed.toISOString())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.planned.set(true);
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          if (error.status === 401) {
            this.auth.clear();
            void this.router.navigate(['/login']);
          } else {
            this.errorKey.set(error.status === 400 ? 'planning.checkDetails' : 'planning.error');
          }
        },
      });
  }
}
