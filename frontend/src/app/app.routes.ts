import { Routes } from '@angular/router';
import { authGuard } from './services/auth.guard';
import { CanDeactivateFn } from '@angular/router';
import { TimerComponent } from './components/timer-component/timer-component';

const leaveTimer: CanDeactivateFn<TimerComponent> = (component) => component.leave();

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
    path: 'sessions/history',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/session-history-component/session-history-component').then(
        ({ SessionHistoryComponent }) => SessionHistoryComponent,
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
    path: 'sessions/:id/timer',
    canActivate: [authGuard],
    canDeactivate: [leaveTimer],
    component: TimerComponent,
  },
  {
    path: 'sessions/plan',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/plan-session-component/plan-session-component').then(
        ({ PlanSessionComponent }) => PlanSessionComponent,
      ),
  },
  {
    path: 'landing',
    redirectTo: '',
    pathMatch: 'full',
  },
];
