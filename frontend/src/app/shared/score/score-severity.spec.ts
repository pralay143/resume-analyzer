import { ScoreSeverityPipe, scoreColor, scoreSeverity } from './score-severity';

describe('scoreSeverity', () => {
  it('maps the boundaries', () => {
    expect(scoreSeverity(0)).toBe('danger');
    expect(scoreSeverity(49)).toBe('danger');
    expect(scoreSeverity(50)).toBe('warn');
    expect(scoreSeverity(74)).toBe('warn');
    expect(scoreSeverity(75)).toBe('success');
    expect(scoreSeverity(100)).toBe('success');
  });

  it('is exposed as a pipe', () => {
    expect(new ScoreSeverityPipe().transform(80)).toBe('success');
  });

  it('gives a theme color for each severity', () => {
    expect(scoreColor(10)).toBe('var(--p-red-500)');
    expect(scoreColor(60)).toBe('var(--p-amber-500)');
    expect(scoreColor(90)).toBe('var(--p-green-500)');
  });
});
