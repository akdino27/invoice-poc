import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './core/layout/navbar/navbar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar],
  template: `
    <app-navbar />
    <main>
      <router-outlet />
    </main>
  `,
  styles: [`
    main { min-height: calc(100vh - 64px); }
  `]
})
export class AppComponent {}