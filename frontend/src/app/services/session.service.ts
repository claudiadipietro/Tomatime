import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

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
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);

  startSpontaneous(duration: number): Observable<StartedSession> {
    return this.http.post<StartedSession>('/api/session-starts', { duration });
  }

  startPlanned(sessionId: number): Observable<void> {
    return this.http.post<void>(`/api/session-starts/${sessionId}`, {});
  }

  plan(duration: number, plannedStartTime: string): Observable<PlannedSession> {
    return this.http.post<PlannedSession>('/api/session-plans', { duration, plannedStartTime });
  }

  heartbeat(id: number): Observable<ActiveSession> {
    return this.http.post<ActiveSession>(`/api/session-runs/${id}/heartbeat`, {});
  }

  cancel(id: number, token: string): Observable<void> {
    return this.http.post<void>(
      `/api/session-runs/${id}/cancel`,
      {},
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) },
    );
  }

  complete(id: number): Observable<{ pomosEarned: number }> {
    return this.http.post<{ pomosEarned: number }>(`/api/session-runs/${id}/complete`, {});
  }
}
