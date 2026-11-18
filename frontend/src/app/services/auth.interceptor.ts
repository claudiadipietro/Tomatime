import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { AuthSessionService } from './auth-session.service';
import { API_ORIGIN, apiUrl } from './api-url';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const url = apiUrl(request.url, inject(API_ORIGIN));
  const authSession = inject(AuthSessionService);
  const authService = inject(AuthService);
  const accessToken = authSession.accessToken;
  const isAuthRequest = request.url.startsWith('/api/auth/');
  const authorizedRequest = request.clone({
    url,
    ...(accessToken && !isAuthRequest
      ? { setHeaders: { Authorization: `Bearer ${accessToken}` } }
      : {}),
  });

  const response = next(authorizedRequest);
  if (isAuthRequest) {
    return response;
  }

  return response.pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse && error.status === 401)) {
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap((response) => next(request.clone({
          url,
          setHeaders: { Authorization: `Bearer ${response.accessToken}` },
        }))),
        catchError((refreshError: unknown) => {
          authSession.clear();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
