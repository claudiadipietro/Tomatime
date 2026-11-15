import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { RewardShopService, RewardShopState } from '../../services/reward-shop.service';
import { RewardOperationService } from '../../services/reward-operation.service';
import { MainHeaderComponent } from '../main-header-component/main-header-component';

@Component({
  selector: 'app-rewards',
  imports: [RouterLink, TranslatePipe, MainHeaderComponent],
  templateUrl: './rewards-component.html',
  styleUrl: './rewards-component.css',
})
export class RewardsComponent {
  private readonly rewards = inject(RewardShopService);
  private readonly operations = inject(RewardOperationService);
  private readonly destroyRef = inject(DestroyRef);
  readonly state = signal<RewardShopState | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly themes = ['default', 'pink', 'blue'] as const;

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.rewards.load().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (state) => { this.state.set(state); this.loading.set(false); },
      error: () => { this.error.set('rewards.loadError'); this.loading.set(false); },
    });
  }

  owned(theme: 'default' | 'pink' | 'blue'): boolean {
    const state = this.state();
    return theme === 'default' || (theme === 'pink' ? !!state?.pinkUnlocked : !!state?.blueUnlocked);
  }

  pending(kind: 'pink' | 'blue' | 'extra-break'): boolean {
    return this.operations.hasPending(kind);
  }

  purchase(kind: 'pink' | 'blue' | 'extra-break'): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.operations.run(kind, (id) => this.rewards.purchase(kind, id))
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({
        next: (state) => this.state.set(state),
        error: (error: HttpErrorResponse) => this.error.set(error.status === 409 ? 'rewards.insufficient' : 'rewards.actionError'),
      });
  }

  activate(theme: 'default' | 'pink' | 'blue'): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.rewards.activate(theme).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false))).subscribe({
      next: (state) => this.state.set(state),
      error: () => this.error.set('rewards.actionError'),
    });
  }
}
