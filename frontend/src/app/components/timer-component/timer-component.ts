import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { catchError, map, Observable, of, Subject, switchMap, take, finalize } from 'rxjs';
import { AuthSessionService } from '../../services/auth-session.service';
import { ActiveSession, SessionService } from '../../services/session.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';
import { RewardOperationService } from '../../services/reward-operation.service';

@Component({
  selector: 'app-timer',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  templateUrl: './timer-component.html',
  styleUrl: './timer-component.css',
})
export class TimerComponent {
  private readonly sessions = inject(SessionService);
  private readonly operations = inject(RewardOperationService);
  private readonly auth = inject(AuthSessionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));
  private readonly token = this.auth.accessToken ?? '';
  private readonly storageKey = `tomatime.timer.${this.id}`;
  private readonly leaveDialog = viewChild.required<ElementRef<HTMLDialogElement>>('leaveDialog');
  private readonly leaveDecision = new Subject<boolean>();
  private readonly now = signal(0);
  private serverAtSync = 0;
  private monotonicAtSync = 0;
  private completing = false;
  private leaving = false;
  private wasBreak = false;

  readonly session = signal<ActiveSession | null>(null);
  readonly state = signal<'loading' | 'active' | 'completed' | 'ended'>('loading');
  readonly cancellationPending = signal(false);
  readonly error = signal<string | null>(null);
  readonly earned = signal(0);
  readonly extending = signal(false);
  get pendingExtension(): boolean {
    return this.operations.hasPending(`session.${this.id}.extra-break`);
  }
  readonly confetti = Array.from({ length: 42 }, (_, index) => index);
  readonly elapsed = computed(() => {
    const session = this.session();
    return session
      ? Math.max(0, Math.floor((this.now() - Date.parse(session.actualStartTime)) / 1000))
      : 0;
  });
  readonly totalSeconds = computed(() => {
    const duration = this.session()?.duration ?? 0;
    const extra = (this.session()?.breakExtensions ?? []).reduce((sum, count) => sum + count, 0);
    return (duration + (Math.max(0, duration / 25 - 1) + extra) * 5) * 60;
  });
  readonly phase = computed(() => {
    const elapsed = this.elapsed();
    const blocks = this.blocks();
    const extensions = this.session()?.breakExtensions ?? [];
    let start = 0;
    for (let i = 0; i < blocks; i++) {
      if (elapsed < start + 1500) return { block: i + 1, isBreak: false, end: start + 1500 };
      start += 1500;
      if (i < blocks - 1) {
        const end = start + 300 * (1 + (extensions[i] ?? 0));
        if (elapsed < end) return { block: i + 1, isBreak: true, end };
        start = end;
      }
    }
    return { block: blocks, isBreak: false, end: start };
  });
  readonly isBreak = computed(() => this.phase().isBreak);
  readonly block = computed(() => this.phase().block);
  readonly blocks = computed(() => (this.session()?.duration ? this.session()!.duration / 25 : 0));
  readonly remaining = computed(() => {
    const elapsed = this.elapsed();
    const phaseEnd = this.phase().end;
    const seconds = Math.max(0, Math.min(phaseEnd, this.totalSeconds()) - elapsed);
    return `${Math.floor(seconds / 60)
      .toString()
      .padStart(2, '0')}:${(seconds % 60).toString().padStart(2, '0')}`;
  });
  readonly progress = computed(() =>
    this.totalSeconds() ? Math.min(100, (this.elapsed() / this.totalSeconds()) * 100) : 0,
  );
  readonly segments = computed(() =>
    Array.from({ length: Math.max(0, this.blocks() * 2 - 1) }, (_, index) => {
      const isBreak = index % 2 === 1;
      const blockIndex = Math.floor(index / 2);
      const extensions = this.session()?.breakExtensions ?? [];
      const previousExtra = extensions.slice(0, blockIndex).reduce((sum, count) => sum + count, 0);
      const startsAt = blockIndex * 1800 + previousExtra * 300 + (isBreak ? 1500 : 0);
      const seconds = isBreak ? 300 * (1 + (extensions[blockIndex] ?? 0)) : 1500;
      return {
        isBreak,
        progress: Math.max(0, Math.min(100, ((this.elapsed() - startsAt) / seconds) * 100)),
      };
    }),
  );

  constructor() {
    if (!Number.isSafeInteger(this.id) || this.id <= 0 || !this.token) {
      this.state.set('ended');
      return;
    }
    if (sessionStorage.getItem(this.storageKey)) {
      this.cancelAfterReload();
      return;
    }
    sessionStorage.setItem(this.storageKey, 'active');
    this.heartbeat();
    const clock = window.setInterval(() => this.tick(), 1000);
    const activity = window.setInterval(() => this.heartbeat(), 30_000);
    this.destroyRef.onDestroy(() => {
      window.clearInterval(clock);
      window.clearInterval(activity);
    });
  }

  private heartbeat(): void {
    if (this.state() === 'completed' || this.state() === 'ended' || this.leaving) return;
    this.sessions
      .heartbeat(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          if (Date.parse(session.serverTime) < this.serverAtSync) return;
          this.serverAtSync = Date.parse(session.serverTime);
          this.monotonicAtSync = performance.now();
          this.now.set(this.serverAtSync);
          this.session.set(session);
          this.state.set('active');
          this.error.set(null);
          this.wasBreak = this.isBreak();
          this.tick();
        },
        error: (error: HttpErrorResponse) => {
          if (this.completing) return;
          if (error.status === 404 || error.status === 401) {
            this.state.set('ended');
            this.error.set('timer.ended');
          } else {
            this.error.set('timer.connectionError');
          }
        },
      });
  }

  private tick(): void {
    if (this.state() !== 'active') return;
    this.now.set(this.serverAtSync + (performance.now() - this.monotonicAtSync));
    const inBreak = this.isBreak();
    const enteredBreak = inBreak && !this.wasBreak;
    this.wasBreak = inBreak;
    if (enteredBreak) this.heartbeat();
    if (this.elapsed() >= this.totalSeconds() && !this.error()) this.complete();
  }

  extendBreak(): void {
    if (this.extending() || (!this.isBreak() && !this.pendingExtension)) return;
    this.extending.set(true);
    this.error.set(null);
    this.operations.run(`session.${this.id}.extra-break`, (id) => this.sessions.extendBreak(this.id, id))
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.extending.set(false)))
      .subscribe({
        next: () => this.heartbeat(),
        error: (error: HttpErrorResponse) =>
          this.error.set(error.status === 409 ? 'rewards.insufficient' : 'rewards.actionError'),
      });
  }

  complete(): void {
    if (this.completing || this.state() !== 'active') return;
    this.completing = true;
    this.sessions
      .complete(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.completing = false;
          this.earned.set(result.pomosEarned);
          this.state.set('completed');
          this.error.set(null);
        },
        error: (error: HttpErrorResponse) => {
          this.completing = false;
          if (error.status === 404 || error.status === 401) {
            this.state.set('ended');
            this.error.set('timer.ended');
          } else {
            this.error.set(error.status === 409 ? 'timer.waiting' : 'timer.completeError');
          }
        },
      });
  }

  leave(): boolean | Observable<boolean> {
    if (this.state() === 'completed' || this.state() === 'ended' || !this.token) return true;
    this.leaveDialog().nativeElement.showModal();
    return this.leaveDecision.pipe(
      take(1),
      switchMap((confirmed) => {
        if (!confirmed) return of(false);
        if (this.state() === 'completed' || this.state() === 'ended') return of(true);
        this.leaving = true;
        this.cancellationPending.set(true);
        return this.sessions.cancel(this.id, this.token).pipe(
          map(() => true),
          catchError((error: HttpErrorResponse) => {
            if (error.status === 404) return of(true);
            this.leaving = false;
            this.error.set('timer.cancelError');
            return of(false);
          }),
        );
      }),
      finalize(() => {
        this.cancellationPending.set(false);
        this.leaveDialog().nativeElement.close();
      }),
    );
  }

  decideLeave(confirmed: boolean): void {
    if (this.cancellationPending()) return;
    this.leaveDecision.next(confirmed);
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.state() === 'completed' || this.state() === 'ended' || !this.token) return;
    event.preventDefault();
    event.returnValue = '';
  }

  @HostListener('window:pagehide')
  onPageHide(): void {
    if (this.state() === 'completed' || this.state() === 'ended' || !this.token) return;
    void fetch(`/api/session-runs/${this.id}/cancel`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${this.token}` },
      keepalive: true,
    });
  }

  private cancelAfterReload(): void {
    this.sessions
      .cancel(this.id, this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.state.set('ended');
          this.error.set('timer.ended');
        },
        error: (error: HttpErrorResponse) => {
          this.state.set('ended');
          this.error.set(error.status === 404 ? 'timer.ended' : 'timer.cancelPending');
        },
      });
  }
}
