import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../../shared/models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private apiUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getPendingUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/users/pending`);
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/users`);
  }

  approveUser(userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${userId}/approve`, {});
  }

  rejectUser(userId: string, reason: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${userId}/reject`, { reason });
  }

  promoteUser(userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${userId}/promote`, {});
  }

  deleteUser(userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/users/${userId}`);
  }

  unlockUser(userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users/${userId}/unlock`, {});
  }
}