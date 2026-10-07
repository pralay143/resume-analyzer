import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';

import { HealthService } from '../../core/services/health.service';

type ApiState = 'checking' | 'connected' | 'unreachable';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, ButtonModule, TagModule],
  template: `
    <section class="hero">
      <h1>AI Resume Analyzer</h1>
      <p class="lead">
        Upload your resume and a job description. You get a match score, the skills you match and miss,
        and specific suggestions to improve your resume for that job.
      </p>

      <div class="actions">
        <p-button label="Analyze a resume" icon="pi pi-search" routerLink="/analyze" />
        <p-button label="View history" icon="pi pi-history" severity="secondary" [outlined]="true" routerLink="/history" />
      </div>

      <div class="status">
        @switch (apiState()) {
          @case ('checking') {
            <p-tag severity="secondary" icon="pi pi-spin pi-spinner" value="Checking API…" />
          }
          @case ('connected') {
            <p-tag severity="success" value="API connected ✓" />
          }
          @case ('unreachable') {
            <p-tag severity="danger" value="API not reachable ✗" />
          }
        }
      </div>
    </section>
  `,
  styles: `
    .hero {
      max-width: 42rem;
      padding: 2rem 0;
    }

    .lead {
      font-size: 1.125rem;
      line-height: 1.6;
      color: var(--p-text-muted-color);
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin: 1.5rem 0;
    }
  `
})
export class HomeComponent implements OnInit {
  private readonly healthService = inject(HealthService);

  readonly apiState = signal<ApiState>('checking');

  ngOnInit(): void {
    this.healthService.check().subscribe({
      next: health => this.apiState.set(health.status === 'ok' ? 'connected' : 'unreachable'),
      error: () => this.apiState.set('unreachable')
    });
  }
}
