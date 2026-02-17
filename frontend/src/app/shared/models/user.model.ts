export interface User {
  id: string;
  email: string;
  role: 'Admin' | 'Vendor';
  status: 'Pending' | 'Approved' | 'Rejected' | 'Locked';
  companyName: string | null;
  address: string | null;
  phoneNumber: string | null;
  driveFolderId: string | null;
  rejectionReason: string | null;
  failedLoginCount: number;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string;
  approvedAt: string | null;
  approvedByAdminId: string | null;
  isSoftDeleted: boolean;
}