import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalyticsService } from '../../core/services/analytics.service';
import { ProductTrend } from '../../shared/models/analytics.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class Dashboard implements OnInit {
  trendingProducts = signal<ProductTrend[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    const end = new Date();
    const start = new Date();
    
    // FIX: Set to year 2000 to capture your 2012 invoices
    start.setFullYear(2000); 

    this.loading.set(true);

    this.analyticsService.getTrendingProducts(
      start.toISOString(), 
      end.toISOString(), 
      5
    ).subscribe({
      next: (data) => {
        this.trendingProducts.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load trending products', err);
        this.error.set('Failed to load analytics data.');
        this.loading.set(false);
      }
    });
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(val);
  }

  formatNumber(val: number): string {
    return new Intl.NumberFormat('en-US').format(val);
  }
}
