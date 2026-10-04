import { DestroyRef, ElementRef, Injector, afterNextRender, inject, signal } from '@angular/core';

/** Local UX cooldown; server rate limits and code expiry remain authoritative. */
export class OtpRecoveryState {
  readonly seconds = signal(0);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private timer: ReturnType<typeof setInterval> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stop());
  }

  start(seconds = 30): void {
    this.stop();
    const until = Date.now() + seconds * 1000;
    this.seconds.set(seconds);
    this.timer = setInterval(() => {
      this.seconds.set(Math.max(0, Math.ceil((until - Date.now()) / 1000)));
      if (this.seconds() === 0) this.stop();
    }, 1000);
  }

  onError(error: unknown): void {
    // Existing API auth limits use a one-minute window.
    if ((error as { statusCode?: number })?.statusCode === 429) this.start(60);
  }

  focus(selector: string): void {
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus(), {
      injector: this.injector,
    });
  }

  private stop(): void {
    if (this.timer !== undefined) clearInterval(this.timer);
    this.timer = undefined;
  }
}
