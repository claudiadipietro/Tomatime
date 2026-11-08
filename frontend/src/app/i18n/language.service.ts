import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export type SupportedLanguage = 'en' | 'es';

const LANGUAGE_STORAGE_KEY = 'tomatime.language';
const SUPPORTED_LANGUAGES: readonly SupportedLanguage[] = ['en', 'es'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly document = inject(DOCUMENT);
  private readonly translate = inject(TranslateService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  readonly currentLanguage = this.translate.currentLang;

  async initialize(): Promise<void> {
    this.translate.addLangs([...SUPPORTED_LANGUAGES]);

    const storedLanguage =
      this.document.defaultView?.localStorage.getItem(LANGUAGE_STORAGE_KEY);
    const language = this.isSupportedLanguage(storedLanguage)
      ? storedLanguage
      : this.getBrowserLanguage();

    await firstValueFrom(this.translate.use(language));
    this.updateDocumentLanguage(language);
  }

  async setLanguage(language: SupportedLanguage): Promise<void> {
    await firstValueFrom(this.translate.use(language));
    this.document.defaultView?.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
    this.updateDocumentLanguage(language);
  }

  private getBrowserLanguage(): SupportedLanguage {
    const browserLanguage =
      this.document.defaultView?.navigator.language.toLowerCase() ?? 'en';

    return browserLanguage.startsWith('es') ? 'es' : 'en';
  }

  private isSupportedLanguage(
    language: string | null | undefined,
  ): language is SupportedLanguage {
    return language === 'en' || language === 'es';
  }

  private updateDocumentLanguage(language: SupportedLanguage): void {
    this.document.documentElement.lang = language;
    this.title.setTitle(this.translate.instant('app.title'));
    this.meta.updateTag({
      name: 'description',
      content: this.translate.instant('app.description'),
    });
  }
}
