export interface Invoice {
  id: string;
  invoiceNumber: string | null;
  invoiceDate: string | null;
  orderId: string | null;
  vendorName: string | null;
  billToName: string | null;
  shipTo: {
    city: string | null;
    state: string | null;
    country: string | null;
  } | null;
  shipMode: string | null;
  subtotal: number | null;
  discount: {
    percentage: number | null;
    amount: number | null;
  } | null;
  shippingCost: number | null;
  totalAmount: number | null;
  balanceDue: number | null;
  currency: string | null;
  notes: string | null;
  terms: string | null;
  lineItems: LineItem[];
  uploadedByVendorId: string;
  // FIXED: Mismatched properties below
  driveFileId: string;        // Was googleDriveFileId
  originalFileName: string;
  createdAt: string;
  updatedAt: string;
}

export interface LineItem {
  id: string;
  productId: string;
  productName: string;
  category: string | null;
  quantity: number;
  unitRate: number;
  amount: number;
}

export interface InvoiceListResponse {
  invoices: Invoice[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}