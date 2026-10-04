import { Component, Input } from '@angular/core';

export const ICON_PATHS = {
  eye: 'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Zm13 0a3 3 0 1 1-6 0 3 3 0 0 1 6 0',
  eyeOff: 'M3 3l18 18M10 5a13 13 0 0 1 12 7s-1 2-3 4M6 6c-3 2-4 6-4 6s3.5 7 10 7c2 0 4-.6 5-1.5M10 10a3 3 0 0 0 4 4',
  edit: 'M15 5l4 4M4 20l4-1 12-12a3 3 0 0 0-4-4L4 15v5Z',
  retry: 'M20 7v5h-5M20 12a8 8 0 1 0-2 5',
  basket: 'M3 9h18l-2 11H5L3 9ZM7 9l3-6m7 6-3-6M9 13v3m6-3v3',
  trash: 'M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7',
  plus: 'M12 5v14M5 12h14', minus: 'M5 12h14',
  search: 'M21 21l-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0',
  user: 'M20 21v-2a6 6 0 0 0-6-6h-4a6 6 0 0 0-6 6v2M16 5a4 4 0 1 1-8 0 4 4 0 0 1 8 0',
  bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',
  receipt: 'M5 3l2 1 2-1 3 1 3-1 2 1 2-1v18l-2-1-2 1-3-1-3 1-2-1-2 1V3ZM9 8h6m-6 4h6m-6 4h4',
  store: 'M3 10V7l2-4h14l2 4v3M3 10c0 4 5 4 5 0 0 4 8 4 8 0 0 4 5 4 5 0M5 13v8h14v-8M9 21v-6h6v6',
  menu: 'M3 6h18M3 12h18M3 18h18', close: 'M6 6l12 12M18 6 6 18',
  upload: 'M12 16V3m-5 5 5-5 5 5M4 16v5h16v-5',
  image: 'M3 3h18v18H3V3Zm0 14 6-6 4 4 3-3 5 5M8 7h.01',
  left: 'M15 5l-7 7 7 7', right: 'M9 5l7 7-7 7',
  check: 'M4 12l5 5L20 6', grid: 'M3 3h7v7H3V3Zm11 0h7v7h-7V3ZM3 14h7v7H3v-7Zm11 0h7v7h-7v-7',
  package: 'M3 6l9-4 9 4v12l-9 4-9-4V6Zm0 0 9 4 9-4M12 10v12M7 4l10 4',
} as const;
export type IconName = keyof typeof ICON_PATHS;

@Component({
  selector: 'app-icon', standalone: true,
  template: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path [attr.d]="paths[name]" /></svg>`,
  styles: [`:host{display:inline-flex;flex-shrink:0;width:1.25rem;height:1.25rem;vertical-align:middle}svg{width:100%;height:100%}:host-context([dir=rtl]):host(.directional){transform:scaleX(-1)}`],
})
export class IconComponent {
  @Input() name: IconName = 'basket';
  readonly paths = ICON_PATHS;
}
