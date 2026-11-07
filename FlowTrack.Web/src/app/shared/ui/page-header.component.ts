import { Component, input } from '@angular/core';

@Component({
  selector: 'ft-page-header',
  standalone: true,
  template: `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <div><h1 class="h3 mb-1">{{ title() }}</h1><p class="text-secondary mb-0">{{ subtitle() }}</p></div>
      <ng-content />
    </div>
  `
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
}
