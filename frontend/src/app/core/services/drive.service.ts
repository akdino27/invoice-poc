import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DriveFile, DriveFileListResponse } from '../models/drive-file.model';

declare const gapi: any;

@Injectable({
  providedIn: 'root'
})
export class DriveService {

  /**
   * List files from the shared folder
   */
  async listFiles(pageSize: number = 50, orderBy: string = 'modifiedTime desc'): Promise<DriveFile[]> {
    try {
      const response = await gapi.client.drive.files.list({
        q: `'${environment.google.sharedFolderId}' in parents and trashed=false`,
        pageSize: pageSize,
        fields: 'files(id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, thumbnailLink, iconLink)',
        orderBy: orderBy
      });

      return response.result.files || [];
    } catch (error) {
      console.error('Error listing files:', error);
      throw error;
    }
  }

  /**
   * Get file metadata
   */
  async getFileMetadata(fileId: string): Promise<DriveFile> {
    try {
      const response = await gapi.client.drive.files.get({
        fileId: fileId,
        fields: 'id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, thumbnailLink, iconLink'
      });

      return response.result;
    } catch (error) {
      console.error('Error getting file metadata:', error);
      throw error;
    }
  }

  /**
   * Delete a file
   */
  async deleteFile(fileId: string): Promise<void> {
    try {
      await gapi.client.drive.files.delete({
        fileId: fileId
      });
    } catch (error) {
      console.error('Error deleting file:', error);
      throw error;
    }
  }

  /**
   * Download file content
   */
  async downloadFile(fileId: string, fileName: string): Promise<void> {
    try {
      const response = await gapi.client.drive.files.get({
        fileId: fileId,
        alt: 'media'
      });

      // Create blob and download
      const blob = new Blob([response.body], { type: response.headers['Content-Type'] });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error downloading file:', error);
      throw error;
    }
  }

  /**
   * Search files by name
   */
  async searchFiles(searchTerm: string): Promise<DriveFile[]> {
    try {
      const query = `'${environment.google.sharedFolderId}' in parents and name contains '${searchTerm}' and trashed=false`;
      
      const response = await gapi.client.drive.files.list({
        q: query,
        pageSize: 50,
        fields: 'files(id, name, mimeType, size, createdTime, modifiedTime, webViewLink, webContentLink, thumbnailLink, iconLink)',
        orderBy: 'modifiedTime desc'
      });

      return response.result.files || [];
    } catch (error) {
      console.error('Error searching files:', error);
      throw error;
    }
  }

  /**
   * Format file size to human-readable format
   */
  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Get file icon based on mime type
   */
  getFileIcon(mimeType: string): string {
    const iconMap: { [key: string]: string } = {
      'application/pdf': '📄',
      'image/jpeg': '🖼️',
      'image/jpg': '🖼️',
      'image/png': '🖼️',
      'image/gif': '🖼️',
      'application/vnd.google-apps.folder': '📁',
      'application/vnd.google-apps.document': '📝',
      'application/vnd.google-apps.spreadsheet': '📊',
      'application/vnd.google-apps.presentation': '📽️',
    };

    return iconMap[mimeType] || '📎';
  }
}
