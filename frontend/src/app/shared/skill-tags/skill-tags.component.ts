import { Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-skill-tags',
  standalone: true,
  imports: [TagModule],
  template: `
    @if (skills().length > 0) {
      <ul class="skill-tags">
        @for (skill of skills(); track skill) {
          <li><p-tag [value]="skill" [severity]="severity()" /></li>
        }
      </ul>
    } @else {
      <span class="none">None</span>
    }
  `,
  styles: `
    .skill-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .none {
      color: var(--p-text-muted-color);
    }
  `
})
export class SkillTagsComponent {
  readonly skills = input.required<string[]>();
  readonly severity = input<'success' | 'warn' | 'danger' | 'secondary' | 'info'>('secondary');
}
