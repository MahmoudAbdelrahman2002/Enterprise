import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-logo',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a class="logo" [routerLink]="link" aria-label="Subito">
      @if (markOnly) {
        <img
          class="mark"
          [src]="light ? '/assets/logo-mark-light.png' : '/assets/logo-mark.png'"
          alt=""
          [style.height.px]="height"
        />
      } @else {
        <img
          class="lockup"
          [src]="light ? '/assets/logo-light.png' : '/assets/logo.png'"
          alt="Subito"
          [style.height.px]="height"
        />
      }
    </a>
  `,
  styles: [
    `
      .logo {
        display: inline-flex;
        align-items: center;
        line-height: 0;
      }
      img {
        width: auto;
        max-width: 100%;
        display: block;
      }
      .mark {
        object-fit: contain;
      }
    `,
  ],
})
export class LogoComponent {
  @Input() link = '/';
  @Input() height = 36;
  /** Use the standalone S mark when space is tight. */
  @Input() markOnly = false;
  /** White/navy-inverted lockup for dark backgrounds. */
  @Input() light = false;
}
