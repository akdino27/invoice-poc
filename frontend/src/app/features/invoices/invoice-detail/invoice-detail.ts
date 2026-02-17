import { Component, OnInit, Input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InvoiceService } from '../../../core/services/invoice.service';
import { Invoice } from '../../../shared/models/invoice.model';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './invoice-detail.html',
  styleUrls: ['./invoice-detail.css']
})
export class InvoiceDetail implements OnInit {
  @Input() id!: string;

  invoice = signal<Invoice | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  // Computed properties
  shipToAddress = computed(() => {
    const inv = this.invoice();
    if (!inv?.shipTo) return null;
    const parts = [
      inv.shipTo.city,
      inv.shipTo.state,
      inv.shipTo.country
    ].filter(Boolean);
    return parts.length > 0 ? parts.join(', ') : null;
  });

  hasDiscount = computed(() => {
    const inv = this.invoice();
    return inv?.discount && (inv.discount.amount || inv.discount.percentage);
  });

  hasShipping = computed(() => {
    const inv = this.invoice();
    return inv?.shippingCost && inv.shippingCost > 0;
  });

  hasBalanceDue = computed(() => {
    const inv = this.invoice();
    return inv?.balanceDue && inv.balanceDue > 0;
  });

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit() {
    if (this.id) {
      this.loadInvoice();
    }
  }

  loadInvoice() {
    this.loading.set(true);
    this.error.set(null);

    this.invoiceService.getInvoiceById(this.id).subscribe({
      next: (invoice) => {
        this.invoice.set(invoice);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load invoice', err);
        this.error.set('Failed to load invoice details. Please try again.');
        this.loading.set(false);
      }
    });
  }

  getSubtotal(): number {
    return this.invoice()?.subtotal || 0;
  }

  getDiscountAmount(): number {
    return this.invoice()?.discount?.amount || 0;
  }

  getDiscountPercentage(): string {
    const percentage = this.invoice()?.discount?.percentage;
    return percentage ? `(${percentage}%)` : '';
  }

  getShippingCost(): number {
    return this.invoice()?.shippingCost || 0;
  }

  getTotalAmount(): number {
    return this.invoice()?.totalAmount || 0;
  }

  getBalanceDue(): number {
    return this.invoice()?.balanceDue || 0;
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
      month: 'long',
      day: 'numeric'
    });
  }
}
