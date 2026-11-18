import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize, forkJoin, map } from 'rxjs';
import { Reward, RewardShopService, RewardShopState } from '../../services/reward-shop.service';
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
  readonly catalog = signal<Reward[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly themes = ['default', 'pink', 'blue'] as const;
  readonly themes = computed(() => [
    'default' as const,
    ...this.catalog().filter((reward): reward is Reward & { name: 'pink' | 'blue' } =>
      reward.type === 0 && (reward.name === 'pink' || reward.name === 'blue'))
      .map(reward => reward.name),
  ]);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({ state: this.rewards.load(), catalog: this.rewards.catalog() })
      .pipe(
        map(result => {
          const expected = [
            { name: 'pink', type: 0 },
            { name: 'blue', type: 0 },
            { name: 'extra-break', type: 1 },
          ];
          const ids = new Set<number>();
          for (const item of expected) {
            const matches = result.catalog.filter(reward => reward.name === item.name);
            const reward = matches[0];
            if (matches.length !== 1 || reward.type !== item.type || !Number.isSafeInteger(reward.id) ||
                !Number.isSafeInteger(reward.cost) || reward.cost < 0 || ids.has(reward.id)) {
              throw new Error('Reward catalog is invalid or incomplete.');
            }
            ids.add(reward.id);
          }
          return result;
        }),
        takeUntilDestroyed(this.destroyRef),
      ).subscribe({
      next: ({ state, catalog }) => {
        this.catalog.set(catalog);
        this.state.set(state);
        this.loading.set(false);
      },
      error: () => { this.state.set(null); this.error.set('rewards.loadError'); this.loading.set(false); },
    });
  }

  reward(kind: 'pink' | 'blue' | 'extra-break'): Reward | undefined {
    return this.catalog().find(reward => reward.name === kind);
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
    const reward = this.reward(kind);
    if (!reward) {
      this.error.set('rewards.actionError');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.operations.run(kind, (id) => this.rewards.purchase(reward.id, id))
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
