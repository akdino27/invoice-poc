import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  isLoading = false;
  errorMessage = '';

  constructor(private authService: AuthService) {}

  async ngOnInit(): Promise<void> {
    // Initialize Google API
    try {
      await this.authService.initialize();
    } catch (error) {
      console.error('Error initializing Google API:', error);
      this.errorMessage = 'Failed to initialize. Please refresh the page.';
    }
  }

  async signInWithGoogle(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      await this.authService.signIn();
    } catch (error) {
      console.error('Sign-in error:', error);
      this.errorMessage = 'Sign-in failed. Please try again.';
      this.isLoading = false;
    }
  }
}
