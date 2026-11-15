import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthSessionService } from './auth-session.service';

@Injectable({ providedIn: 'root' })
export class RewardOperationService {
  private readonly auth = inject(AuthSessionService);

  private key(action: string): string {
    const userId = this.auth.currentUser()?.id;
    if (userId == null) throw new Error('An authenticated user is required for a reward purchase.');
    return `tomatime.reward-operation.${userId}.${action}`;
  }

  hasPending(action: string): boolean {
    return this.auth.currentUser() !== null && localStorage.getItem(this.key(action)) !== null;
  }

  run<T>(action: string, request: (operationId: string) => Observable<T>): Observable<T> {
    const key = this.key(action);
    const operationId = localStorage.getItem(key) ?? crypto.randomUUID();
    localStorage.setItem(key, operationId);
    return request(operationId).pipe(tap({
      next: () => localStorage.removeItem(key),
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500 &&
            error.status !== 408 && error.status !== 429) {
          localStorage.removeItem(key);
        }
      },
    }));
  }
}
