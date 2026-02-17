import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpEventType } from '@angular/common/http';
import { Router } from '@angular/router';
import { InvoiceService } from '../../core/services/invoice.service';

interface UploadFileState {
  name: string;
  size: number;
  progress: number;
  status: 'uploading' | 'success' | 'error';
  error?: string;
}

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './upload.html',
  styleUrls: ['./upload.css'],
})
export class Upload {
  isDragging = signal(false);
  uploadedFiles = signal<UploadFileState[]>([]);
  isUploading = signal(false);

  constructor(private invoiceService: InvoiceService, private router: Router) {}

  onDragOver(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();
    this.isDragging.set(false);
    
    if (e.dataTransfer?.files) {
      this.handleFiles(e.dataTransfer.files);
    }
  }

  onFileSelect(e: Event) {
    const input = e.target as HTMLInputElement;
    if (input.files) {
      this.handleFiles(input.files);
    }
  }

  handleFiles(fileList: FileList) {
    // Process one by one or batch. Here we process individually.
    Array.from(fileList).forEach(file => {
      // Validate Type
      const validTypes = ['application/pdf', 'image/png', 'image/jpeg', 'image/jpg'];
      if (!validTypes.includes(file.type)) {
        alert(`Invalid type: ${file.name}`);
        return;
      }
      
      this.uploadSingleFile(file);
    });
  }

  uploadSingleFile(file: File) {
    this.isUploading.set(true);
    
    // Add to list
    const fileState: UploadFileState = {
      name: file.name,
      size: file.size,
      progress: 0,
      status: 'uploading'
    };
    
    this.uploadedFiles.update(list => [...list, fileState]);
    
    this.invoiceService.uploadFile(file).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const progress = Math.round((100 * event.loaded) / event.total);
          this.updateFileState(file.name, { progress });
        } else if (event.type === HttpEventType.Response) {
          this.updateFileState(file.name, { status: 'success', progress: 100 });
          this.isUploading.set(false);
        }
      },
      error: (err) => {
        const errorMsg = err.error?.Message || 'Upload failed';
        this.updateFileState(file.name, { status: 'error', error: errorMsg });
        this.isUploading.set(false);
      }
    });
  }

  updateFileState(fileName: string, updates: Partial<UploadFileState>) {
    this.uploadedFiles.update(files => 
      files.map(f => f.name === fileName ? { ...f, ...updates } : f)
    );
  }

  removeFile(name: string) {
    this.uploadedFiles.update(files => files.filter(f => f.name !== name));
  }

  clearAll() {
    this.uploadedFiles.set([]);
  }

  goToDashboard() {
    this.router.navigate(['/dashboard']);
  }

  // Helpers
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getStatusIcon(status: string) {
    if (status === 'success') return 'âœ“';
    if (status === 'error') return '!';
    return 'â†‘';
  }

  getStatusColor(status: string) {
    if (status === 'success') return '#22c55e';
    if (status === 'error') return '#ef4444';
    return '#3b82f6';
  }

  hasSuccessfulUploads() {
    return this.uploadedFiles().some(f => f.status === 'success');
  }
}