import { Routes } from '@angular/router';

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
    path: 'landing',
    redirectTo: '',
    pathMatch: 'full',
  },
];
