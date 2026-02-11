export interface DriveFile {
  id: string;
  name: string;
  mimeType: string;
  size?: string;
  createdTime: string;
  modifiedTime: string;
  webViewLink?: string;
  webContentLink?: string;
  thumbnailLink?: string;
  iconLink?: string;
}

export interface DriveFileListResponse {
  kind: string;
  incompleteSearch: boolean;
  files: DriveFile[];
  nextPageToken?: string;
}

export interface DriveUploadResponse {
  id: string;
  name: string;
  mimeType: string;
  kind: string;
}

export interface UploadProgress {
  fileName: string;
  progress: number;
  status: 'uploading' | 'completed' | 'error';
  error?: string;
}
