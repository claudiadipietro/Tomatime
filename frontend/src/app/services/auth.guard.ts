import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from './auth-session.service';

export const authGuard: CanActivateFn = () =>
  inject(AuthSessionService).accessToken ? true : inject(Router).createUrlTree(['/login']);
