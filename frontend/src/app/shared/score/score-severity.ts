import { Pipe, PipeTransform } from '@angular/core';

export type ScoreSeverity = 'danger' | 'warn' | 'success';

/** Below 50 is a weak match, 50–74 a partial match, 75 and above a strong match. */
export function scoreSeverity(score: number): ScoreSeverity {
  if (score >= 75) {
    return 'success';
  }
  return score >= 50 ? 'warn' : 'danger';
}

const severityColors: Record<ScoreSeverity, string> = {
  danger: 'var(--p-red-500)',
  warn: 'var(--p-amber-500)',
  success: 'var(--p-green-500)'
};

/** A theme color for the score, for components that take a color rather than a severity. */
export function scoreColor(score: number): string {
  return severityColors[scoreSeverity(score)];
}

@Pipe({ name: 'scoreSeverity', standalone: true })
export class ScoreSeverityPipe implements PipeTransform {
  transform(score: number): ScoreSeverity {
    return scoreSeverity(score);
  }
}
