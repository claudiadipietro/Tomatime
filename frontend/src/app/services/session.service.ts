import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RewardShopState } from './reward-shop.service';

export interface StartedSession {
  id: number;
  duration: number;
  actualStartTime: string;
}

export interface PlannedSession {
  id: number;
  duration: number;
  plannedStartTime: string;
}

export interface ActiveSession {
  id: number;
  duration: number;
  actualStartTime: string;
  serverTime: string;
  breakExtensions: number[];
  pomoBalance: number;
  extraBreakCost: number;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);

  startSpontaneous(duration: number): Observable<StartedSession> {
    return this.http.post<StartedSession>('/api/sessions', { duration });
  }

  startPlanned(sessionId: number): Observable<void> {
    return this.http.post<void>(`/api/sessions/${sessionId}/start`, {});
  }

  plan(duration: number, plannedStartTime: string): Observable<PlannedSession> {
    return this.http.post<PlannedSession>('/api/sessions', { duration, plannedStartTime });
  }

  heartbeat(id: number): Observable<ActiveSession> {
    return this.http.post<ActiveSession>(`/api/sessions/${id}/heartbeat`, {});
  }

  cancel(id: number, token: string): Observable<void> {
    return this.http.post<void>(
      `/api/sessions/${id}/cancel`,
      {},
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) },
    );
  }

  complete(id: number): Observable<{ pomosEarned: number }> {
    return this.http.post<{ pomosEarned: number }>(`/api/sessions/${id}/complete`, {});
  }

  extendBreak(id: number, operationId: string): Observable<RewardShopState> {
    return this.http.post<RewardShopState>(`/api/sessions/${id}/extra-break`, { operationId });
  }
}
