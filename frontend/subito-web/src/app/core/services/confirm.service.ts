import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly request = signal<{ messageKey: string; target?: string; actionKey: string; accept: (ok: boolean) => void } | null>(null);

  ask(messageKey = 'confirm.delete', target?: string, actionKey = 'actions.delete'): Promise<boolean> {
    // Resolve an older request before replacing it so no caller remains pending.
    this.request()?.accept(false);
    return new Promise((accept) => this.request.set({ messageKey, target, actionKey, accept }));
  }

  answer(ok: boolean): void {
    const current = this.request();
    current?.accept(ok);
    this.request.set(null);
  }
}
