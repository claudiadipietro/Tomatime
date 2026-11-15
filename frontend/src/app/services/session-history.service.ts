import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SessionHistoryItem } from '../models/session-history.model';

@Injectable({ providedIn: 'root' })
export class SessionHistoryService {
  private readonly http = inject(HttpClient);

  getHistory(): Observable<SessionHistoryItem[]> {
    return this.http.get<SessionHistoryItem[]>('/api/session-history');
  }

  getItem(id: number): Observable<SessionHistoryItem> {
    return this.http.get<SessionHistoryItem>(`/api/session-history/${id}`);
  }
}
