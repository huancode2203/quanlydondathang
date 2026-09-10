import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-toolbar',
  standalone: true,
  template: `
    <header class="ui-page-toolbar">
      <div class="ui-page-toolbar__copy">
        <h2>{{ title }}</h2>
        @if (description) { <p>{{ description }}</p> }
      </div>
      <div class="ui-page-toolbar__actions">
        <ng-content />
      </div>
    </header>
  `,
})
export class PageToolbarComponent {
  @Input({ required: true }) title = '';
  @Input() description = '';
}
