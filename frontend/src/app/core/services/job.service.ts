import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Job, JobListResponse } from '../../shared/models/job.model';
import { environment } from '../../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class JobService {
  private apiUrl = `${environment.apiUrl}/jobs`;

  constructor(private http: HttpClient) {}

  getJobs(
    status?: string,
    page: number = 1,
    pageSize: number = 50
  ): Observable<JobListResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<JobListResponse>(this.apiUrl, { params });
  }

  getJobById(id: string): Observable<Job> {
    return this.http.get<Job>(`${this.apiUrl}/${id}`);
  }

  requeueJob(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/requeue`, {});
  }
}
