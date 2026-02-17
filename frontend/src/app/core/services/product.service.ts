import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, ProductListResponse, Category } from '../../shared/models/product.model';
import { environment } from '../../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getProducts(
    category?: string,
    search?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<ProductListResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (category) {
      params = params.set('category', category);
    }
    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<ProductListResponse>(this.apiUrl, { params });
  }

  getProductById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  getProductByCode(productId: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/by-code/${productId}`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`);
  }
}
