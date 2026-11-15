import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthSessionService } from './auth-session.service';
import { API_ORIGIN, apiUrl } from './api-url';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const url = apiUrl(request.url, inject(API_ORIGIN));
  const accessToken = inject(AuthSessionService).accessToken;
  if (!accessToken || request.url.startsWith('/api/auth/')) {
    return next(request.clone({ url }));
  }

  return next(
    request.clone({
      url,
      setHeaders: { Authorization: `Bearer ${accessToken}` },
    }),
  );
};
