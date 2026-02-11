import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

declare const google: any;
declare const gapi: any;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUser = signal<User | null>(null);
  private accessToken = signal<string | null>(null);
  private tokenClient: any;
  private gapiInitialized = false;

  readonly user = this.currentUser.asReadonly();
  readonly token = this.accessToken.asReadonly();

  constructor(private router: Router) {
    this.loadUserFromStorage();
  }

  /**
   * Initialize Google API and Identity Services
   */
  async initialize(): Promise<void> {
    return new Promise((resolve, reject) => {
      // Initialize Google API Client
      gapi.load('client', async () => {
        try {
          await gapi.client.init({
            discoveryDocs: environment.google.discoveryDocs,
          });
          this.gapiInitialized = true;

          // Initialize Google Identity Services
          this.tokenClient = google.accounts.oauth2.initTokenClient({
            client_id: environment.google.clientId,
            scope: environment.google.scopes.join(' '),
            callback: (response: any) => {
              if (response.error) {
                console.error('Token error:', response.error);
                reject(response.error);
                return;
              }
              this.handleTokenResponse(response);
              resolve();
            },
          });

          resolve();
        } catch (error) {
          console.error('Error initializing Google API:', error);
          reject(error);
        }
      });
    });
  }

  /**
   * Sign in with Google
   */
  async signIn(): Promise<void> {
    if (!this.tokenClient) {
      await this.initialize();
    }

    return new Promise((resolve, reject) => {
      try {
        // Request access token
        this.tokenClient.callback = async (response: any) => {
          if (response.error) {
            console.error('Sign-in error:', response.error);
            reject(response.error);
            return;
          }

          await this.handleTokenResponse(response);
          resolve();
        };

        this.tokenClient.requestAccessToken({ prompt: 'consent' });
      } catch (error) {
        console.error('Sign-in error:', error);
        reject(error);
      }
    });
  }

  /**
   * Handle token response and fetch user info
   */
  private async handleTokenResponse(response: any): Promise<void> {
    const token = response.access_token;
    this.accessToken.set(token);
    sessionStorage.setItem('access_token', token);

    // Set token for gapi
    gapi.client.setToken({ access_token: token });

    // Fetch user info
    await this.fetchUserInfo(token);

    // Navigate to dashboard
    this.router.navigate(['/dashboard']);
  }

  /**
   * Fetch user information from Google
   */
  private async fetchUserInfo(token: string): Promise<void> {
    try {
      const response = await fetch('https://www.googleapis.com/oauth2/v2/userinfo', {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch user info');
      }

      const userInfo = await response.json();
      
      const user: User = {
        id: userInfo.id,
        email: userInfo.email,
        name: userInfo.name,
        picture: userInfo.picture,
        givenName: userInfo.given_name,
        familyName: userInfo.family_name
      };

      this.currentUser.set(user);
      sessionStorage.setItem('user', JSON.stringify(user));
    } catch (error) {
      console.error('Error fetching user info:', error);
      throw error;
    }
  }

  /**
   * Sign out
   */
  signOut(): void {
    // Revoke token
    const token = this.accessToken();
    if (token) {
      google.accounts.oauth2.revoke(token, () => {
        console.log('Token revoked');
      });
    }

    // Clear state
    this.currentUser.set(null);
    this.accessToken.set(null);
    sessionStorage.removeItem('access_token');
    sessionStorage.removeItem('user');

    // Reset gapi token
    if (this.gapiInitialized) {
      gapi.client.setToken(null);
    }

    // Navigate to login
    this.router.navigate(['/login']);
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.accessToken() !== null && this.currentUser() !== null;
  }

  /**
   * Get current access token
   */
  getAccessToken(): string | null {
    return this.accessToken();
  }

  /**
   * Load user and token from storage
   */
  private loadUserFromStorage(): void {
    const token = sessionStorage.getItem('access_token');
    const userJson = sessionStorage.getItem('user');

    if (token && userJson) {
      this.accessToken.set(token);
      this.currentUser.set(JSON.parse(userJson));

      // Set token for gapi if initialized
      if (typeof gapi !== 'undefined' && gapi.client) {
        gapi.client.setToken({ access_token: token });
      }
    }
  }

  /**
   * Refresh token if needed
   */
  async refreshTokenIfNeeded(): Promise<void> {
    const token = this.accessToken();
    if (!token) {
      return;
    }

    // Check if token is still valid by making a test request
    try {
      const response = await fetch('https://www.googleapis.com/oauth2/v2/userinfo', {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      if (!response.ok) {
        // Token expired, request new one
        await this.signIn();
      }
    } catch (error) {
      console.error('Error checking token validity:', error);
      // Request new token
      await this.signIn();
    }
  }
}
