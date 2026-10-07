import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';

import { WAKE_TOAST_KEY } from './core/interceptors/slow-request.interceptor';
import { HealthService } from './core/services/health.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule],
  template: `
    <header class="app-header">
      <a routerLink="/" class="brand">
        <i class="pi pi-file-check" aria-hidden="true"></i>
        Resume Analyzer
      </a>
      <nav>
        @for (item of navItems; track item.path) {
          <a [routerLink]="item.path" routerLinkActive="active">
            <i [class]="item.icon" aria-hidden="true"></i>
            {{ item.label }}
          </a>
        }
      </nav>
    </header>

    <main class="app-main">
      <router-outlet />
    </main>

    <!-- Error toasts from the HTTP interceptor. Confirm dialogs live in the pages that use them. -->
    <p-toast position="top-right" />
    <p-toast position="bottom-center" [key]="wakeToastKey" />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly healthService = inject(HealthService);

  title = 'resume-analyzer-ui';
  readonly wakeToastKey = WAKE_TOAST_KEY;

  ngOnInit(): void {
    // Starts waking a sleeping free-tier server as soon as the site opens, before the user needs it.
    this.healthService.check().subscribe({ error: () => undefined });
  }

  readonly navItems = [
    { label: 'Analyze', icon: 'pi pi-search', path: '/analyze' },
    { label: 'History', icon: 'pi pi-history', path: '/history' },
    { label: 'Dashboard', icon: 'pi pi-chart-bar', path: '/dashboard' }
  ];
}
