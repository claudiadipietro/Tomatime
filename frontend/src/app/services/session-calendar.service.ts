import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarSession } from '../models/calendar-session.model';

@Injectable({ providedIn: 'root' })
export class SessionCalendarService {
  private readonly http = inject(HttpClient);

  getSessions(from: Date, to: Date): Observable<CalendarSession[]> {
    const params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
    return this.http.get<CalendarSession[]>('/api/sessions/calendar', { params });
  }
}
