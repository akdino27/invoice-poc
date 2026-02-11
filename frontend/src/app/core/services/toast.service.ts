import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toasts = signal<Toast[]>([]);
  readonly toast$ = this.toasts.asReadonly();

  success(message: string, duration: number = 3000): void {
    this.show('success', message, duration);
  }

  error(message: string, duration: number = 4000): void {
    this.show('error', message, duration);
  }

  info(message: string, duration: number = 3000): void {
    this.show('info', message, duration);
  }

  warning(message: string, duration: number = 3000): void {
    this.show('warning', message, duration);
  }

  private show(type: ToastType, message: string, duration: number): void {
    const id = Math.random().toString(36).substring(7);
    const toast: Toast = { id, type, message, duration };

    this.toasts.update(toasts => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }
  }

  remove(id: string): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  clear(): void {
    this.toasts.set([]);
  }
}