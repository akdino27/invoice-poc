import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../shared/models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.html',
  styleUrls: ['./product-list.css']
})
export class ProductList implements OnInit {
  products = signal<Product[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Pagination
  currentPage = signal(1);
  pageSize = signal(20);
  totalProducts = signal(0);
  totalPages = signal(0);

  // Filters
  selectedCategory = signal<string>('');
  searchTerm = signal<string>('');
  categories = signal<string[]>([]);

  constructor(private productService: ProductService) {}

  ngOnInit() {
    this.loadProducts();
    this.loadCategories();
  }

  loadProducts() {
    this.loading.set(true);
    this.error.set(null);

    const category = this.selectedCategory() || undefined;
    const search = this.searchTerm() || undefined;

    this.productService.getProducts(category, search, this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        this.products.set(response.products);
        this.totalProducts.set(response.total);
        this.totalPages.set(Math.ceil(response.total / this.pageSize()));
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load products', err);
        this.error.set('Failed to load products. Please try again.');
        this.loading.set(false);
      }
    });
  }

  loadCategories() {
    this.productService.getCategories().subscribe({
      next: (cats) => {
        this.categories.set(cats.map(c => c.category));
      },
      error: (err) => {
        console.error('Failed to load categories', err);
      }
    });
  }

  onCategoryChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    this.selectedCategory.set(target.value);
    this.currentPage.set(1);
    this.loadProducts();
  }

  onSearchChange(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchTerm.set(target.value);
    this.currentPage.set(1);
    // Debounce search
    setTimeout(() => {
      if (this.searchTerm() === target.value) {
        this.loadProducts();
      }
    }, 500);
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadProducts();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadProducts();
    }
  }

  formatCurrency(value: number | null): string {
    if (value === null) return 'N/A';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return 'Never';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
