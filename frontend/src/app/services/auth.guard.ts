import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';
import { AuthSessionService } from './auth-session.service';

export const authGuard: CanActivateFn = () => {
  const authSession = inject(AuthSessionService);
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authSession.accessToken) {
    return true;
  }

  return authService.restoreSession().pipe(
    map(() => authSession.accessToken ? true : router.createUrlTree(['/login'])),
  );
};
