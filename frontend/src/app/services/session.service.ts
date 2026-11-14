import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface StartedSession {
  id: number;
  duration: number;
  actualStartTime: string;
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
}
