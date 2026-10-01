import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="app-header">
      <a routerLink="/" class="brand">Resume Analyzer</a>
      <nav>
        <a routerLink="/analyze" routerLinkActive="active">Analyze</a>
        <a routerLink="/history" routerLinkActive="active">History</a>
        <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
      </nav>
    </header>
    <main class="app-main">
      <router-outlet />
    </main>
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'resume-analyzer-ui';
}
