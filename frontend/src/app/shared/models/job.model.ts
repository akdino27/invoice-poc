export interface Job {
  id: string;
  jobType: string;
  status: 'PENDING' | 'PROCESSING' | 'COMPLETED' | 'FAILED' | 'INVALID';
  retryCount: number;
  lockedBy: string | null;
  lockedAt: string | null;
  nextRetryAt: string | null;
  createdAt: string;
  updatedAt: string;
  payloadJson: any | null;
  errorMessage: any | null;
}

export interface JobListResponse {
  jobs: Job[];
  page: number;
  pageSize: number;
  total: number;
}