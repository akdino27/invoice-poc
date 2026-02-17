import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './signup.html',
  styleUrl: './signup.css',
})
export class Signup {
  email: string = '';
  password: string = '';
  confirmPassword: string = '';
  companyName: string = '';
  address: string = '';
  phoneNumber: string = '';
  loading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private auth: Auth,
    private router: Router
  ) {}

  signup(): void {
    this.errorMessage = '';
    this.successMessage = '';

    // Validation
    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    if (!this.email || !this.password || !this.companyName) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    this.loading = true;

    this.auth.signup({
      email: this.email,
      password: this.password,
      companyName: this.companyName,
      address: this.address || undefined,
      phoneNumber: this.phoneNumber || undefined
    }).subscribe({
      next: (res) => {
        console.log('Signup success', res);
        this.successMessage = res.message || 'Signup successful! Please wait for admin approval.';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 3000);
      },
      error: (err) => {
        console.error('Signup failed', err);
        this.loading = false;
        
        if (err.status === 400) {
          this.errorMessage = err.error?.message || 'Invalid signup data';
        } else if (err.status === 429) {
          this.errorMessage = 'Too many signup attempts. Please try again later.';
        } else if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'An error occurred during signup';
        }
      }
    });
  }
}
