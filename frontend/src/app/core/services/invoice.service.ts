import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Invoice, InvoiceListResponse } from '../../shared/models/invoice.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private apiUrl = `${environment.apiUrl}/invoices`;
  // Based on Backend V7, VendorInvoicesController handles uploads
  private uploadUrl = `${environment.apiUrl}/VendorInvoices/upload`;

  constructor(private http: HttpClient) {}

  getInvoices(page: number = 1, pageSize: number = 20): Observable<InvoiceListResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<InvoiceListResponse>(this.apiUrl, { params });
  }

  getInvoiceById(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.apiUrl}/${id}`);
  }

  uploadFile(file: File): Observable<HttpEvent<any>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<any>(this.uploadUrl, formData, {
      reportProgress: true,
      observe: 'events'
    });
  }
}