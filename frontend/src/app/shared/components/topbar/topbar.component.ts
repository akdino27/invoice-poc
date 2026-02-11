import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.css']
})
export class TopbarComponent {
  @Output() toggleSidebar = new EventEmitter<void>();

  private authService = inject(AuthService);
  private themeService = inject(ThemeService);
  
  user = this.authService.user;
  theme = this.themeService.theme;
  showUserMenu = false;

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
  }

  closeUserMenu(): void {
    this.showUserMenu = false;
  }

  logout(): void {
    this.closeUserMenu();
    if (confirm('Are you sure you want to logout?')) {
      this.authService.signOut();
    }
  }
}