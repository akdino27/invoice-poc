import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, TopbarComponent],
  templateUrl: './layout.component.html',
  styles: [`
    @media (max-width: 768px) {
      .ml-64, .ml-16 { margin-left: 0 !important; }
    }
  `]
})
export class LayoutComponent {
  isSidebarOpen = true;

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }
}