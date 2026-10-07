import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { AnalysisListItem } from '../../core/models/analysis.models';
import { AnalysisService } from '../../core/services/analysis.service';
import { ScoreSeverityPipe } from '../../shared/score/score-severity';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [DatePipe, RouterLink, ButtonModule, ConfirmDialogModule, TableModule, TagModule, ScoreSeverityPipe],
  templateUrl: './history.component.html',
  styleUrl: './history.component.scss'
})
export class HistoryComponent {
  private readonly analysisService = inject(AnalysisService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = 10;

  readonly items = signal<AnalysisListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly loadedOnce = signal(false);

  private first = 0;

  /** Called by the table on first render and whenever the page changes. */
  load(event?: TableLazyLoadEvent): void {
    this.first = event?.first ?? this.first;
    const rows = event?.rows ?? this.pageSize;
    const page = Math.floor(this.first / rows) + 1;

    this.loading.set(true);
    this.analysisService
      .getPage(page, rows)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          // A delete can empty the last page; step back to the previous one.
          if (result.items.length === 0 && result.totalCount > 0 && page > 1) {
            this.load({ first: this.first - rows, rows });
            return;
          }
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
          this.loadedOnce.set(true);
        },
        error: () => {
          this.loading.set(false);
          this.loadedOnce.set(true);
        }
      });
  }

  open(item: AnalysisListItem): void {
    this.router.navigate(['/analysis', item.id]);
  }

  confirmDelete(item: AnalysisListItem, event: Event): void {
    // The delete button sits inside a clickable row.
    event.stopPropagation();

    this.confirmation.confirm({
      header: 'Delete analysis',
      message: `Delete the analysis for "${item.jobTitle}"? This can't be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () =>
        this.analysisService
          .delete(item.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({ next: () => this.load(), error: () => undefined })
    });
  }
}
