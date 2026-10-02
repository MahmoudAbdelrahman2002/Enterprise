import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal, effect } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type Lang = 'en' | 'ar' | 'it';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly storageKey = 'subito.lang';
  private readonly dict = signal<Record<string, string>>({});
  readonly lang = signal<Lang>(this.readInitial());
  readonly dir = computed(() => (this.lang() === 'ar' ? 'rtl' : 'ltr'));

  constructor(private readonly http: HttpClient) {
    effect(() => {
      const lang = this.lang();
      document.documentElement.lang = lang;
      document.documentElement.dir = this.dir();
      localStorage.setItem(this.storageKey, lang);
      void this.load(lang);
    });
  }

  t(key: string, fallback?: string): string {
    return this.dict()[key] ?? fallback ?? key;
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
  }

  private readInitial(): Lang {
    const saved = localStorage.getItem(this.storageKey) as Lang | null;
    return saved === 'ar' || saved === 'it' || saved === 'en' ? saved : 'en';
  }

  private async load(lang: Lang): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.http.get<Record<string, string>>(`/assets/i18n/${lang}.json`)
      );
      if (this.lang() === lang) this.dict.set(data);
    } catch {
      if (this.lang() === lang) this.dict.set({});
    }
  }
}
