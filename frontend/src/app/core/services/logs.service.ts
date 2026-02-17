import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LogListResponse, LogStats } from '../../shared/models/log.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private apiUrl = `${environment.apiUrl}/logs`;

  constructor(private http: HttpClient) {}

  getLogs(page: number = 1, pageSize: number = 50, changeType?: string): Observable<LogListResponse> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (changeType) params = params.set('changeType', changeType);
    return this.http.get<LogListResponse>(this.apiUrl, { params });
  }

  getLogStats(): Observable<LogStats> {
    return this.http.get<LogStats>(`${this.apiUrl}/stats`);
  }
}