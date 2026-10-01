import { Component, input } from '@angular/core';

@Component({
  selector: 'app-analysis-detail',
  standalone: true,
  template: `<h1>Analysis {{ id() }}</h1><p>Coming soon.</p>`
})
export class AnalysisDetailComponent {
  readonly id = input<string>();
}
