import { Routes } from '@angular/router';
import { authGuard } from './services/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/landing-page-component/landing-page-component').then(
        ({ LandingPageComponent }) => LandingPageComponent,
      ),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./components/login-component/login-component').then(
        ({ LoginComponent }) => LoginComponent,
      ),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./components/register-component/register-component').then(
        ({ RegisterComponent }) => RegisterComponent,
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./components/reset-password-component/reset-password-component').then(
        ({ ResetPasswordComponent }) => ResetPasswordComponent,
      ),
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/main-screen-component/main-screen-component').then(
        ({ MainScreenComponent }) => MainScreenComponent,
      ),
  },
  {
    path: 'sessions/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/new-session-component/new-session-component').then(
        ({ NewSessionComponent }) => NewSessionComponent,
      ),
  },
  {
    path: 'landing',
    redirectTo: '',
    pathMatch: 'full',
  },
];
