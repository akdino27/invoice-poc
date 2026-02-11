import { Component, Input, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

interface MenuItem {
  label: string;
  icon: SafeHtml;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styles: [`
    @media (max-width: 768px) {
      aside.-translate-x-full { transform: translateX(-100%); }
      aside.translate-x-0 { transform: translateX(0); }
    }
  `]
})
export class SidebarComponent {
  @Input() isOpen = true;
  isMobile = false;
  
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  menuItems: MenuItem[] = [
    { 
      label: 'Dashboard', 
      route: '/dashboard',
      icon: this.sanitizer.bypassSecurityTrustHtml(`
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"></path>
        </svg>
      `) 
    },
    { 
      label: 'Upload Invoice', 
      route: '/upload',
      icon: this.sanitizer.bypassSecurityTrustHtml(`
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"></path>
        </svg>
      `)
    },
    { 
      label: 'Files', 
      route: '/files',
      icon: this.sanitizer.bypassSecurityTrustHtml(`
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"></path>
        </svg>
      `)
    }
  ];

  constructor() {
    this.checkScreenSize();
  }

  @HostListener('window:resize')
  checkScreenSize() {
    this.isMobile = window.innerWidth < 768;
  }

  logout(): void {
    if (confirm('Are you sure you want to logout?')) {
      this.authService.signOut();
    }
  }
}