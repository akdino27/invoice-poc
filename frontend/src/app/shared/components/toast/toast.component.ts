import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.css']
})
export class ToastComponent {
  private toastService = inject(ToastService);
  toasts = this.toastService.toast$;

  getIcon(type: string): string {
    const icons: { [key: string]: string } = {
      success: '✓',
      error: '✕',
      info: 'ℹ',
      warning: '⚠'
    };
    return icons[type] || 'ℹ';
  }

  close(id: string): void {
    this.toastService.remove(id);
  }
}