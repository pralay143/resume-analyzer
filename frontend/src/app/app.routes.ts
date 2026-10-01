import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
    title: 'Resume Analyzer'
  },
  {
    path: 'analyze',
    loadComponent: () => import('./features/analyze/analyze.component').then(m => m.AnalyzeComponent),
    title: 'Analyze'
  },
  {
    path: 'history',
    loadComponent: () => import('./features/history/history.component').then(m => m.HistoryComponent),
    title: 'History'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    title: 'Dashboard'
  },
  {
    path: 'analysis/:id',
    loadComponent: () =>
      import('./features/analysis-detail/analysis-detail.component').then(m => m.AnalysisDetailComponent),
    title: 'Analysis'
  },
  { path: '**', redirectTo: '' }
];
