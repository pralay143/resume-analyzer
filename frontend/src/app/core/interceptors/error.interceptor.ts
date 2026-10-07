import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';

import { ProblemDetails } from '../models/analysis.models';

/**
 * Shows the API's error message as a toast, then rethrows so callers can still react.
 * 404s are left to the page, which knows what "not found" means there.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const messages = inject(MessageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const message = describeHttpError(error);
        if (message) {
          messages.add({ severity: 'error', summary: message.summary, detail: message.detail, life: 8000 });
        }
      }
      return throwError(() => error);
    })
  );
};

export interface ErrorMessage {
  summary: string;
  detail: string;
}

/** Turns an API error into a user-facing message, or null when the page should handle it. */
export function describeHttpError(error: HttpErrorResponse): ErrorMessage | null {
  const problem = asProblemDetails(error.error);

  switch (error.status) {
    case 0:
      return {
        summary: 'Server unreachable',
        detail: "Can't reach the server. Check your connection, or try again in a moment."
      };
    case 400:
      return { summary: problem?.title ?? 'Invalid request', detail: validationDetail(problem) };
    case 404:
      return null;
    case 413:
      return { summary: 'File too large', detail: problem?.detail ?? 'The upload is too large. Resumes can be at most 5 MB.' };
    case 429:
      return { summary: 'Too many requests', detail: problem?.detail ?? rateLimitDetail(error) };
    case 502:
      return { summary: 'AI analysis failed', detail: problem?.detail ?? 'The AI service is unavailable right now. Please try again.' };
    default:
      return {
        summary: problem?.title ?? 'Something went wrong',
        detail: problem?.detail ?? 'An unexpected error occurred. Please try again later.'
      };
  }
}

function asProblemDetails(body: unknown): ProblemDetails | null {
  if (typeof body === 'string') {
    try {
      body = JSON.parse(body);
    } catch {
      return null;
    }
  }
  return body && typeof body === 'object' ? (body as ProblemDetails) : null;
}

function validationDetail(problem: ProblemDetails | null): string {
  const fieldMessages = Object.values(problem?.errors ?? {}).flat();
  if (fieldMessages.length > 0) {
    return fieldMessages.join(' ');
  }
  return problem?.detail ?? 'Please check the form and try again.';
}

function rateLimitDetail(error: HttpErrorResponse): string {
  const seconds = Number(error.headers?.get('Retry-After'));
  if (Number.isFinite(seconds) && seconds > 0) {
    const minutes = Math.ceil(seconds / 60);
    return `You've reached the analysis limit. Please try again in ${minutes} minute${minutes === 1 ? '' : 's'}.`;
  }
  return "You've reached the analysis limit. Please try again later.";
}
