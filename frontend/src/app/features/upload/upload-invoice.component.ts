import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { FileUploadService } from '../../core/services/file-upload.service';
import { UploadProgress } from '../../core/models/drive-file.model';
import { DragDropDirective } from '../../shared/directives/drag-drop.directive';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-upload-invoice',
  standalone: true,
  imports: [CommonModule, DragDropDirective],
  templateUrl: './upload-invoice.component.html',
  styleUrls: ['./upload-invoice.component.css']
})
export class UploadInvoiceComponent implements OnInit, OnDestroy {
  uploadProgress: UploadProgress[] = [];
  isUploading = false;

  private uploadSubscription?: Subscription;

  constructor(
    private fileUploadService: FileUploadService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.uploadSubscription = this.fileUploadService.uploadProgress$.subscribe(
      progress => {
        this.uploadProgress = progress;
        this.isUploading = progress.some(p => p.status === 'uploading');
      }
    );
  }

  ngOnDestroy(): void {
    this.uploadSubscription?.unsubscribe();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFiles(input.files);
    }
  }

  onFilesDropped(files: FileList): void {
    this.uploadFiles(files);
  }

  async uploadFiles(files: FileList): Promise<void> {
    try {
      await this.fileUploadService.uploadFiles(files);
      this.toastService.success(`Successfully uploaded ${files.length} file(s)!`);
      
      setTimeout(() => {
        this.router.navigate(['/files']);
      }, 1500);
    } catch (error) {
      console.error('Upload error:', error);
      this.toastService.error(error instanceof Error ? error.message : 'Upload failed');
    }
  }

  clearCompleted(): void {
    this.fileUploadService.clearCompletedUploads();
  }

  getAcceptAttribute(): string {
    return this.fileUploadService.getAcceptAttribute();
  }

  getProgressPercentage(progress: UploadProgress): number {
    return Math.round(progress.progress);
  }

  getProgressColor(progress: UploadProgress): string {
    if (progress.status === 'completed') return '#10b981';
    if (progress.status === 'error') return '#ef4444';
    return '#0066cc';
  }
}