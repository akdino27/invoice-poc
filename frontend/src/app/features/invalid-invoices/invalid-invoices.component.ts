import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { InvalidInvoiceService, InvalidInvoice } from '../../core/services/invalid-invoice.service';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-invalid-invoices',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatPaginatorModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  templateUrl: './invalid-invoices.component.html',
  styles: [`
    .container { padding: 20px; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    table { width: 100%; }
    .error-cell { color: #f44336; max-width: 300px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  `]
})
export class InvalidInvoicesComponent implements OnInit {
  displayedColumns: string[] = ['fileName', 'failedAt', 'retryCount', 'errorMessage'];
  dataSource: InvalidInvoice[] = [];
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;
  isAdmin = false;

  private invalidInvoiceService = inject(InvalidInvoiceService);
  private auth = inject(Auth);
  private snackBar = inject(MatSnackBar);

  ngOnInit() {
    this.isAdmin = this.auth.getUserRole() === 'Admin';

    // Avoid pushing twice if component re-inits for any reason
    if (this.isAdmin && !this.displayedColumns.includes('actions')) {
      this.displayedColumns = [...this.displayedColumns, 'actions'];
    }

    this.loadData();
  }

  loadData() {
    this.invalidInvoiceService.getInvalidInvoices(this.pageIndex + 1, this.pageSize)
      .subscribe({
        next: (res) => {
          this.dataSource = res.data;
          this.totalCount = res.totalCount;
        },
        error: (err) => console.error('Error loading invalid invoices', err)
      });
  }

  onPageChange(event: PageEvent) {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadData();
  }

  requeueJob(id: string) {
    if (!confirm('Are you sure you want to requeue this job?')) return;

    this.invalidInvoiceService.requeueJob(id).subscribe({
      next: () => {
        this.snackBar.open('Job requeued successfully', 'Close', { duration: 3000 });
        this.loadData();
      },
      error: (err) => {
        this.snackBar.open(
          'Failed to requeue job: ' + (err.error?.message || 'Unknown error'),
          'Close',
          { duration: 5000 }
        );
      }
    });
  }
}
