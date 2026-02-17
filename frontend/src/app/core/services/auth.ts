import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SignupRequest {
  email: string;
  password: string;
  companyName: string;
  address?: string;
  phoneNumber?: string;
  driveFolderId?: string;
}

export interface LoginResult {
  accessToken: string;
  expiresAt: string;
}

type JwtPayload = Record<string, any>;

@Injectable({ providedIn: 'root' })
export class Auth {
  private apiUrl = `${environment.apiUrl}/auth`;

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  login(data: LoginRequest): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${this.apiUrl}/login`, data).pipe(
      tap(result => this.setToken(result.accessToken))
    );
  }

  signup(data: SignupRequest): Observable<{ message: string; email: string }> {
    return this.http.post<{ message: string; email: string }>(
      `${this.apiUrl}/signup`,
      data
    );
  }

  getToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('token');
  }

  setToken(token: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.setItem('token', token);
  }

  clearToken(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem('token');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  // Added for role-based UI (Admin/Vendor)
  getUserRole(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;

    const token = this.getToken();
    if (!token) return null;

    const payload = this.decodeJwtPayload(token);
    if (!payload) return null;

    // Common claim keys
    return (
      payload['role'] ||
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] ||
      null
    );
  }

  private decodeJwtPayload(token: string): JwtPayload | null {
    try {
      const parts = token.split('.');
      if (parts.length < 2) return null;

      const base64Url = parts[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');

      const json = atob(padded);
      return JSON.parse(json);
    } catch {
      return null;
    }
  }
}
