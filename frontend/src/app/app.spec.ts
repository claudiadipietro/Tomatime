import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { App } from './app';
import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({ template: '' })
class PageComponent {}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideTranslateService({ fallbackLang: 'en', lang: 'en' })],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});

describe('App language picker placement', () => {
  it('moves the language picker out of the public toolbar on the dashboard', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([
          { path: '', component: PageComponent },
          { path: 'home', component: PageComponent },
          { path: 'login', component: PageComponent },
        ]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.app-toolbar app-language-picker')).toBeTruthy();

    await router.navigateByUrl('/home?test=1');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.app-toolbar')).toBeNull();

    await router.navigateByUrl('/login');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.app-toolbar app-language-picker')).toBeTruthy();
  });
});
