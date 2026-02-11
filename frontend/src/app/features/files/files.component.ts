import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // ← Make sure this line exists
import { DriveService } from '../../core/services/drive.service';
import { DriveFile } from '../../core/models/drive-file.model';
import { ToastService } from '../../core/services/toast.service';


@Component({
  selector: 'app-files',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './files.component.html',
  styleUrls: ['./files.component.css']
})
export class FilesComponent implements OnInit {
  files: DriveFile[] = [];
  filteredFiles: DriveFile[] = [];
  isLoading = false;
  searchTerm = '';

  
  constructor(
  private driveService: DriveService,
  private toastService: ToastService
) {}

  async ngOnInit(): Promise<void> {
    await this.loadFiles();
  }
  async loadFiles(): Promise<void> {
  this.isLoading = true;

  try {
    this.files = await this.driveService.listFiles(50);
    this.filteredFiles = [...this.files];
    this.toastService.success('Files loaded successfully');
  } catch (error) {
    console.error('Error loading files:', error);
    this.toastService.error('Failed to load files. Please try again.');
  } finally {
    this.isLoading = false;
  }
}


  async onSearch(): Promise<void> {
    if (!this.searchTerm.trim()) {
      this.filteredFiles = [...this.files];
      return;
    }

    try {
      this.filteredFiles = await this.driveService.searchFiles(this.searchTerm);
    } catch (error) {
      console.error('Search error:', error);
    }
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.filteredFiles = [...this.files];
  }

  async downloadFile(file: DriveFile): Promise<void> {
  try {
    await this.driveService.downloadFile(file.id, file.name);
    this.toastService.success(`Downloading ${file.name}`);
  } catch (error) {
    console.error('Download error:', error);
    this.toastService.error('Failed to download file');
  }
}


  async deleteFile(file: DriveFile): Promise<void> {
  if (!confirm(`Are you sure you want to delete "${file.name}"?`)) {
    return;
  }

  try {
    await this.driveService.deleteFile(file.id);
    this.toastService.success(`Deleted ${file.name}`);
    await this.loadFiles();
  } catch (error) {
    console.error('Delete error:', error);
    this.toastService.error('Failed to delete file');
  }
}

  formatFileSize(size: string | undefined): string {
    if (!size) return 'N/A';
    return this.driveService.formatFileSize(parseInt(size, 10));
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getFileIcon(mimeType: string): string {
    return this.driveService.getFileIcon(mimeType);
  }

  openInDrive(file: DriveFile): void {
    if (file.webViewLink) {
      window.open(file.webViewLink, '_blank');
    }
  }

  async refresh(): Promise<void> {
    await this.loadFiles();
  }
}
