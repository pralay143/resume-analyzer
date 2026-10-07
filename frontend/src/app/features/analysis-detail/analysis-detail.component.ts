import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { KnobModule } from 'primeng/knob';
import { PanelModule } from 'primeng/panel';
import { SkeletonModule } from 'primeng/skeleton';

import { Analysis } from '../../core/models/analysis.models';
import { AnalysisService } from '../../core/services/analysis.service';
import { scoreColor } from '../../shared/score/score-severity';
import { SkillTagsComponent } from '../../shared/skill-tags/skill-tags.component';

type LoadState = 'loading' | 'loaded' | 'not-found' | 'error';

@Component({
  selector: 'app-analysis-detail',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    KnobModule,
    PanelModule,
    SkeletonModule,
    SkillTagsComponent
  ],
  templateUrl: './analysis-detail.component.html',
  styleUrl: './analysis-detail.component.scss'
})
export class AnalysisDetailComponent implements OnInit {
  private readonly analysisService = inject(AnalysisService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Bound from the :id route parameter. */
  readonly id = input.required<string>();

  readonly state = signal<LoadState>('loading');
  readonly analysis = signal<Analysis | null>(null);
  readonly deleting = signal(false);

  readonly scoreColor = computed(() => scoreColor(this.analysis()?.matchScore ?? 0));

  ngOnInit(): void {
    this.analysisService
      .getById(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: analysis => {
          this.analysis.set(analysis);
          this.state.set('loaded');
        },
        error: (error: unknown) =>
          this.state.set(error instanceof HttpErrorResponse && error.status === 404 ? 'not-found' : 'error')
      });
  }

  confirmDelete(): void {
    const analysis = this.analysis();
    if (!analysis) {
      return;
    }

    this.confirmation.confirm({
      header: 'Delete analysis',
      message: `Delete the analysis for "${analysis.jobTitle}"? This can't be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => this.delete(analysis.id)
    });
  }

  private delete(id: string): void {
    this.deleting.set(true);
    this.analysisService
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.router.navigate(['/history']),
        error: () => this.deleting.set(false)
      });
  }
}
