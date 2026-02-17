import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Theme } from '../../services/theme';
import { Auth } from '../../services/auth';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, MatSlideToggleModule, CommonModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css'],
})
export class Navbar {
  constructor(
    public theme: Theme,
    public auth: Auth,
    private router: Router
  ) {}

  toggleTheme() {
    this.theme.toggle();
  }

  logout() {
    this.auth.clearToken();
    this.router.navigate(['/login']);
  }
}
