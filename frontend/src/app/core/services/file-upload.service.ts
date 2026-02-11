import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UploadProgress } from '../models/drive-file.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {
  private uploadProgressSubject = new BehaviorSubject<UploadProgress[]>([]);
  public uploadProgress$: Observable<UploadProgress[]> = this.uploadProgressSubject.asObservable();

  private readonly ALLOWED_TYPES = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
  private readonly MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

  constructor(private authService: AuthService) {}

  /**
   * Validate file before upload
   */
  validateFile(file: File): { valid: boolean; error?: string } {
    // Check file type
    if (!this.ALLOWED_TYPES.includes(file.type)) {
      return {
        valid: false,
        error: `File type not allowed. Allowed types: PDF, JPG, PNG`
      };
    }

    // Check file size
    if (file.size > this.MAX_FILE_SIZE) {
      return {
        valid: false,
        error: `File size exceeds 10MB limit`
      };
    }

    return { valid: true };
  }

  /**
   * Upload file to Google Drive
   */
  async uploadFile(file: File): Promise<string> {
    // Validate file
    const validation = this.validateFile(file);
    if (!validation.valid) {
      throw new Error(validation.error);
    }

    const accessToken = this.authService.getAccessToken();
    if (!accessToken) {
      throw new Error('No access token available');
    }

    // Initialize progress
    const progress: UploadProgress = {
      fileName: file.name,
      progress: 0,
      status: 'uploading'
    };
    this.addProgress(progress);

    try {
      // Create metadata
      const metadata = {
        name: file.name,
        mimeType: file.type,
        parents: [environment.google.sharedFolderId]
      };

      // Prepare multipart request
      const boundary = '-------314159265358979323846';
      const delimiter = "\r\n--" + boundary + "\r\n";
      const closeDelimiter = "\r\n--" + boundary + "--";

      // Read file as base64
      const fileContent = await this.readFileAsBase64(file);

      // Build multipart body
      const multipartRequestBody =
        delimiter +
        'Content-Type: application/json; charset=UTF-8\r\n\r\n' +
        JSON.stringify(metadata) +
        delimiter +
        'Content-Type: ' + file.type + '\r\n' +
        'Content-Transfer-Encoding: base64\r\n\r\n' +
        fileContent +
        closeDelimiter;

      // Upload using XMLHttpRequest for progress tracking
      const fileId = await this.uploadWithProgress(
        multipartRequestBody,
        boundary,
        accessToken,
        (progressEvent) => {
          const percentComplete = Math.round((progressEvent.loaded / progressEvent.total) * 100);
          this.updateProgress(file.name, percentComplete);
        }
      );

      // Mark as completed
      this.updateProgress(file.name, 100, 'completed');

      return fileId;
    } catch (error) {
      console.error('Upload error:', error);
      this.updateProgress(file.name, 0, 'error', error instanceof Error ? error.message : 'Upload failed');
      throw error;
    }
  }

  /**
   * Upload multiple files
   */
  async uploadFiles(files: FileList | File[]): Promise<string[]> {
    const fileArray = Array.from(files);
    const uploadPromises = fileArray.map(file => this.uploadFile(file));

    try {
      const fileIds = await Promise.all(uploadPromises);
      return fileIds;
    } catch (error) {
      console.error('Error uploading files:', error);
      throw error;
    }
  }

  /**
   * Upload with progress tracking using XMLHttpRequest
   */
  private uploadWithProgress(
    body: string,
    boundary: string,
    accessToken: string,
    onProgress: (event: ProgressEvent) => void
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      xhr.upload.addEventListener('progress', onProgress);

      xhr.addEventListener('load', () => {
        if (xhr.status === 200) {
          const response = JSON.parse(xhr.responseText);
          resolve(response.id);
        } else {
          reject(new Error(`Upload failed with status ${xhr.status}`));
        }
      });

      xhr.addEventListener('error', () => {
        reject(new Error('Upload failed'));
      });

      xhr.open('POST', 'https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart');
      xhr.setRequestHeader('Authorization', `Bearer ${accessToken}`);
      xhr.setRequestHeader('Content-Type', `multipart/related; boundary=${boundary}`);

      xhr.send(body);
    });
  }

  /**
   * Read file as base64
   */
  private readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        const base64 = result.split(',')[1];
        resolve(base64);
      };
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  }

  /**
   * Add progress entry
   */
  private addProgress(progress: UploadProgress): void {
    const current = this.uploadProgressSubject.value;
    this.uploadProgressSubject.next([...current, progress]);
  }

  /**
   * Update progress entry
   */
  private updateProgress(
    fileName: string,
    progress: number,
    status?: 'uploading' | 'completed' | 'error',
    error?: string
  ): void {
    const current = this.uploadProgressSubject.value;
    const updated = current.map(item => {
      if (item.fileName === fileName) {
        return {
          ...item,
          progress,
          status: status || item.status,
          error: error || item.error
        };
      }
      return item;
    });
    this.uploadProgressSubject.next(updated);
  }

  /**
   * Clear completed uploads
   */
  clearCompletedUploads(): void {
    const current = this.uploadProgressSubject.value;
    const filtered = current.filter(item => item.status === 'uploading');
    this.uploadProgressSubject.next(filtered);
  }

  /**
   * Clear all uploads
   */
  clearAllUploads(): void {
    this.uploadProgressSubject.next([]);
  }

  /**
   * Get allowed file types
   */
  getAllowedTypes(): string[] {
    return this.ALLOWED_TYPES;
  }

  /**
   * Get allowed file extensions for input accept attribute
   */
  getAcceptAttribute(): string {
    return '.pdf,.jpg,.jpeg,.png';
  }
}
