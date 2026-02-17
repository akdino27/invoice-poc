import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InvoiceService } from '../../../core/services/invoice.service';
import { Invoice } from '../../../shared/models/invoice.model';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './invoice-list.html',
  styleUrls: ['./invoice-list.css']
})
export class InvoiceList implements OnInit {
  invoices = signal<Invoice[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Pagination
  currentPage = signal(1);
  pageSize = signal(20);
  totalInvoices = signal(0);
  totalPages = signal(0);

  // Filters
  searchTerm = signal('');
  sortBy = signal('invoiceDate');
  sortOrder = signal<'asc' | 'desc'>('desc');

  // Expose Math to template
  Math = Math;

  // Computed sorted/filtered invoices
  filteredInvoices = computed(() => {
    let result = this.invoices();

    // Apply search filter
    const search = this.searchTerm().toLowerCase();
    if (search) {
      result = result.filter(inv => 
        inv.invoiceNumber?.toLowerCase().includes(search) ||
        inv.vendorName?.toLowerCase().includes(search) ||
        inv.billToName?.toLowerCase().includes(search) ||
        inv.orderId?.toLowerCase().includes(search)
      );
    }

    // Apply sorting
    const sortField = this.sortBy();
    const order = this.sortOrder();
    
    result = [...result].sort((a, b) => {
      let aVal: any = a[sortField as keyof Invoice];
      let bVal: any = b[sortField as keyof Invoice];

      // Handle null values
      if (aVal === null) return 1;
      if (bVal === null) return -1;

      // Handle dates
      if (sortField === 'invoiceDate' || sortField === 'createdAt') {
        aVal = new Date(aVal).getTime();
        bVal = new Date(bVal).getTime();
      }

      // Handle numbers
      if (sortField === 'totalAmount') {
        aVal = Number(aVal) || 0;
        bVal = Number(bVal) || 0;
      }

      if (aVal < bVal) return order === 'asc' ? -1 : 1;
      if (aVal > bVal) return order === 'asc' ? 1 : -1;
      return 0;
    });

    return result;
  });

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit() {
    this.loadInvoices();
  }

  loadInvoices() {
    this.loading.set(true);
    this.error.set(null);

    this.invoiceService.getInvoices(this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        this.invoices.set(response.invoices);
        this.totalInvoices.set(response.total);
        this.totalPages.set(response.totalPages);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load invoices', err);
        this.error.set('Failed to load invoices. Please try again.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    // Search is handled by computed signal
    // Just trigger UI update
  }

  onPageChange(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadInvoices();
  }

  getPaginationRange(): number[] {
    const current = this.currentPage();
    const total = this.totalPages();
    const range: number[] = [];
    
    const delta = 2; // Pages to show on each side of current page
    
    for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
      range.push(i);
    }
    
    if (current - delta > 2) {
      range.unshift(-1); // Ellipsis
    }
    if (current + delta < total - 1) {
      range.push(-1); // Ellipsis
    }
    
    range.unshift(1);
    if (total > 1) {
      range.push(total);
    }
    
    return range;
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadInvoices();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadInvoices();
    }
  }

  formatCurrency(value: number | null): string {
    if (value === null) return 'N/A';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
