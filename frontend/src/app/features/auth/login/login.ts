import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email: string = '';
  password: string = '';
  loading: boolean = false;
  errorMessage: string = '';

  constructor(
    private auth: Auth,
    private router: Router
  ) {}

  login(): void {
    this.loading = true;
    this.errorMessage = '';

    this.auth.login({ 
      email: this.email, 
      password: this.password 
    }).subscribe({
      next: (res) => {
        console.log('Login success', res);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.error('Login failed', err);
        this.loading = false;
        
        if (err.status === 401) {
          this.errorMessage = 'Invalid email or password';
        } else if (err.status === 429) {
          this.errorMessage = 'Too many login attempts. Please try again later.';
        } else if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'An error occurred during login';
        }
      }
    });
  }
}
