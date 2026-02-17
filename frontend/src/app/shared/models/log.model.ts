export interface FileChangeLog {
  id: number;
  fileName: string | null;
  fileId: string | null;
  changeType: string | null;
  detectedAt: string;
  mimeType: string | null;
  fileSize: number | null;
  modifiedBy: string | null;
  googleDriveModifiedTime: string | null;
  processed: boolean;
  processedAt: string | null;
  uploadedByVendorId: string | null;
}

export interface LogStats {
  totalFiles: number;
  totalProcessed: number;
  totalPending: number;
  byChangeType: {
    changeType: string;
    count: number;
    processed: number;
    pending: number;
  }[];
}

export interface LogListResponse {
  logs: FileChangeLog[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}