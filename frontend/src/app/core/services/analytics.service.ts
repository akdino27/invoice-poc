import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProductSales {
  ProductId: string;
  ProductName: string;
  Category: string;
  TotalQuantity: number;
  TotalRevenue: number;
  InvoiceCount: number;
  AverageUnitRate: number;
}

export interface CategorySales {
  Category: string;
  ProductCount: number;
  TotalQuantity: number;
  TotalRevenue: number;
  InvoiceCount: number;
  AverageOrderValue: number;
}

export interface ProductTrend {
  ProductId: string;
  ProductName: string;
  Category: string;
  TotalQuantity: number;
  TotalRevenue: number;
  InvoiceCount: number;
  GrowthRate: number;
  Rank: number;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Analytics`;

  getProductSales(startDate: Date, endDate: Date): Observable<ProductSales[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());
    return this.http.get<ProductSales[]>(`${this.apiUrl}/products/sales`, { params });
  }

  getCategorySales(startDate: Date, endDate: Date): Observable<CategorySales[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());
    return this.http.get<CategorySales[]>(`${this.apiUrl}/categories/sales`, { params });
  }

  getTrendingProducts(startDate: Date, endDate: Date, topN: number = 5): Observable<ProductTrend[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString())
      .set('topN', topN);
    return this.http.get<ProductTrend[]>(`${this.apiUrl}/products/trending`, { params });
  }
}