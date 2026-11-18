import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from '../models/auth.models';
import { AuthSessionService } from './auth-session.service';

const AUTH_API_URL = '/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly authSession = inject(AuthSessionService);
  private refreshRequest: Observable<AuthResponse> | null = null;

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${AUTH_API_URL}/login`, request, { withCredentials: true })
      .pipe(tap((response) => this.authSession.save(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${AUTH_API_URL}/register`, request, { withCredentials: true })
      .pipe(tap((response) => this.authSession.save(response)));
  }

  refreshSession(): Observable<AuthResponse> {
    if (!this.refreshRequest) {
      this.refreshRequest = this.http
        .post<AuthResponse>(`${AUTH_API_URL}/refresh`, {}, {
          withCredentials: true,
          headers: { 'X-Tomatime-Refresh': '1' },
        })
        .pipe(
          tap((response) => this.authSession.save(response)),
          finalize(() => (this.refreshRequest = null)),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    return this.refreshRequest;
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${AUTH_API_URL}/logout`, {}, {
        withCredentials: true,
        headers: { 'X-Tomatime-Refresh': '1' },
      })
      .pipe(finalize(() => this.authSession.clear()));
  }

  restoreSession(): Observable<void> {
    if (this.authSession.accessToken) {
      return of(undefined);
    }

    return this.refreshSession().pipe(
      map(() => undefined),
      catchError(() => {
        this.authSession.clear();
        return of(undefined);
      }),
    );
  }

  requestPasswordReset(request: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${AUTH_API_URL}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${AUTH_API_URL}/reset-password`, request);
  }
}
