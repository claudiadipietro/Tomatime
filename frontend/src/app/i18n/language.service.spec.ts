import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideTranslateService({ fallbackLang: 'en', lang: 'en' })],
    });
  });

  it('switches language and updates document metadata', async () => {
    const languageService = TestBed.inject(LanguageService);
    const translate = TestBed.inject(TranslateService);

    translate.setTranslation(
      'en',
      {
        app: {
          title: 'Tomatime | Focus timer',
          description: 'Organize focused work sessions.',
        },
      },
      true,
    );
    translate.setTranslation(
      'es',
      {
        app: {
          title: 'Tomatime | Temporizador',
          description: 'Organiza tus sesiones de concentración.',
        },
      },
      true,
    );

    await languageService.setLanguage('es');

    expect(languageService.currentLanguage()).toBe('es');
    expect(document.documentElement.lang).toBe('es');
    expect(document.title).toBe('Tomatime | Temporizador');
    expect(document.querySelector<HTMLMetaElement>('meta[name="description"]')?.content).toBe(
      'Organiza tus sesiones de concentración.',
    );
    expect(localStorage.getItem('tomatime.language')).toBe('es');
  });
});
