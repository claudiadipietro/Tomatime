import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface RewardShopState {
  pomoBalance: number;
  pendingExtraBreaks: number;
  currentTheme: string;
  pinkUnlocked: boolean;
  blueUnlocked: boolean;
}

export interface Reward {
  id: number;
  name: string;
  cost: number;
  type: number;
}

@Injectable({ providedIn: 'root' })
export class RewardShopService {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);
  readonly themeError = signal(false);

  load(): Observable<RewardShopState> {
    return this.http.get<RewardShopState>('/api/users/rewards').pipe(tap({
      next: (state) => this.apply(state),
      error: () => this.themeError.set(true),
    }));
  }

  catalog(): Observable<Reward[]> {
    return this.http.get<Reward[]>('/api/rewards');
  }

  purchase(rewardId: number, operationId: string): Observable<RewardShopState> {
    return this.http.post<RewardShopState>(`/api/rewards/${rewardId}/redeem`, { operationId })
      .pipe(tap((state) => this.apply(state)));
  }

  activate(theme: 'default' | 'pink' | 'blue'): Observable<RewardShopState> {
    return this.http.put<RewardShopState>('/api/users/theme', { theme })
      .pipe(tap((state) => this.apply(state)));
  }

  reset(): void {
    delete this.document.documentElement.dataset['theme'];
    this.themeError.set(false);
  }

  private apply(state: RewardShopState): void {
    this.document.documentElement.dataset['theme'] = state.currentTheme;
    this.themeError.set(false);
  }
}
