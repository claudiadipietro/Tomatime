import { Routes } from '@angular/router';
import { authGuard } from './services/auth.guard';
import { TimerComponent } from './components/timer-component/timer-component';

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
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/profile-component/profile-component').then(
        ({ ProfileComponent }) => ProfileComponent,
      ),
  },
  {
    path: 'rewards',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/rewards-component/rewards-component').then(
        ({ RewardsComponent }) => RewardsComponent,
      ),
  },
  {
    path: 'sessions/calendar',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/session-calendar-component/session-calendar-component').then(
        ({ SessionCalendarComponent }) => SessionCalendarComponent,
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
    path: 'sessions/history/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/session-detail-component/session-detail-component').then(
        ({ SessionDetailComponent }) => SessionDetailComponent,
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
