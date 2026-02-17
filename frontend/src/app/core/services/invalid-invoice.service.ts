import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface InvalidInvoice {
  id: string;
  fileName: string;
  vendorId: string;
  errorMessage: string;
  createdAt: string;
  failedAt: string;
  retryCount: number;
  status: string;
}

export interface InvalidInvoiceResponse {
  data: InvalidInvoice[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class InvalidInvoiceService {
  private apiUrl = `${environment.apiUrl}/invalid-invoices`;

  constructor(private http: HttpClient) {}

  getInvalidInvoices(page: number = 1, pageSize: number = 20): Observable<InvalidInvoiceResponse> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<InvalidInvoiceResponse>(this.apiUrl, { params });
  }

  requeueJob(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/requeue`, {});
  }
}
