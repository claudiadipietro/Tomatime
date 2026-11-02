import { Routes } from '@angular/router';

export const routes: Routes = [
	{
		path: '',
		loadComponent: () =>
			import('../components/landing-page-component/landing-page-component').then(
				({ LandingPageComponent }) => LandingPageComponent
			),
	},
	{
		path: 'landing',
		redirectTo: '',
		pathMatch: 'full',
	},
];
