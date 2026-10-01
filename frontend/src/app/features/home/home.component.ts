import { Component, OnInit, inject, signal } from '@angular/core';
import { TagModule } from 'primeng/tag';

import { HealthService } from '../../core/services/health.service';

type ApiState = 'checking' | 'connected' | 'unreachable';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [TagModule],
  template: `
    <h1>AI Resume Analyzer</h1>
    <p>Upload a resume and a job description to get a match score and suggestions.</p>

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
