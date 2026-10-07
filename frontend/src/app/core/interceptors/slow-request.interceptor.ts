import { HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { finalize } from 'rxjs';

/** Toast key for the notice, so it can be cleared without touching error toasts. */
export const WAKE_TOAST_KEY = 'wake';

/** A GET slower than this suggests the free-tier server is waking from sleep. */
export const SLOW_REQUEST_MS = 5000;

/**
 * Shows one "waking up" notice while any slow GET is pending. POSTs are skipped: an analysis
 * normally takes 10–30 seconds and already shows its own progress.
 */
@Injectable({ providedIn: 'root' })
export class WakeNotice {
  private readonly messages = inject(MessageService);
  private pending = 0;

  show(): void {
    if (this.pending++ === 0) {
      this.messages.add({
        key: WAKE_TOAST_KEY,
        severity: 'info',
        summary: 'Waking up the server',
        detail: 'The server sleeps when idle and can take up to a minute to start. Hang tight…',
        sticky: true
      });
    }
  }

  hide(): void {
    if (this.pending > 0 && --this.pending === 0) {
      this.messages.clear(WAKE_TOAST_KEY);
    }
  }
}

export const slowRequestInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') {
    return next(req);
  }

  const notice = inject(WakeNotice);
  let shown = false;
  const timer = setTimeout(() => {
    shown = true;
    notice.show();
  }, SLOW_REQUEST_MS);

  return next(req).pipe(
    finalize(() => {
      clearTimeout(timer);
      if (shown) {
        notice.hide();
      }
    })
  );
};
