import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductSales, ProductTrend } from '../../shared/models/analytics.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getProductSales(startDate: string, endDate: string, category?: string): Observable<ProductSales[]> {
    let params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    if (category) params = params.set('category', category);
    return this.http.get<ProductSales[]>(`${this.apiUrl}/products/sales`, { params });
  }

  getTrendingProducts(startDate: string, endDate: string, topN: number = 5): Observable<ProductTrend[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate).set('topN', topN);
    return this.http.get<ProductTrend[]>(`${this.apiUrl}/products/trending`, { params });
  }
}