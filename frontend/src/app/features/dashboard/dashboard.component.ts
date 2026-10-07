import { DecimalPipe, formatDate } from '@angular/common';
import { Component, DestroyRef, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ChartModule } from 'primeng/chart';
import { SkeletonModule } from 'primeng/skeleton';

import { AnalysisStats } from '../../core/models/analysis.models';
import { AnalysisService } from '../../core/services/analysis.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DecimalPipe, RouterLink, ButtonModule, CardModule, ChartModule, SkeletonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly analysisService = inject(AnalysisService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly locale = inject(LOCALE_ID);

  readonly stats = signal<AnalysisStats | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);

  readonly trendData = computed(() => {
    const trend = this.stats()?.scoreTrend ?? [];
    return {
      labels: trend.map(point => formatDate(point.date, 'MMM d', this.locale)),
      datasets: [
        {
          label: 'Match score',
          data: trend.map(point => point.score),
          borderColor: cssVar('--p-primary-color', '#10b981'),
          backgroundColor: 'transparent',
          tension: 0.3,
          pointRadius: 4
        }
      ]
    };
  });

  readonly missingSkillsData = computed(() => {
    const skills = this.stats()?.topMissingSkills ?? [];
    return {
      labels: skills.map(s => s.skill),
      datasets: [
        {
          label: 'Analyses missing this skill',
          data: skills.map(s => s.count),
          backgroundColor: cssVar('--p-red-400', '#f87171'),
          borderRadius: 4
        }
      ]
    };
  });

  readonly trendOptions = {
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { min: 0, max: 100, ticks: { callback: (value: number | string) => `${value}%` } } }
  };

  readonly missingSkillsOptions = {
    indexAxis: 'y',
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { x: { beginAtZero: true, ticks: { precision: 0 } } }
  };

  ngOnInit(): void {
    this.analysisService
      .getStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: stats => {
          this.stats.set(stats);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }
}

/**
 * Reads a theme color so the charts follow the PrimeNG theme. Chart.js draws on a canvas, which can't use
 * CSS variables, and theme variables often point at other variables, so the chain is followed to a real color.
 */
function cssVar(name: string, fallback: string): string {
  const styles = getComputedStyle(document.documentElement);
  let value = styles.getPropertyValue(name).trim();
  for (let depth = 0; depth < 5; depth++) {
    const reference = /^var\((--[\w-]+)/.exec(value);
    if (!reference) {
      break;
    }
    value = styles.getPropertyValue(reference[1]).trim();
  }
  return value && !value.startsWith('var(') ? value : fallback;
}
