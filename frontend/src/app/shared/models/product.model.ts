export interface Product {
  id: string;
  productId: string;
  productName: string;
  category: string | null;
  primaryCategory: string | null;
  secondaryCategory: string | null;
  defaultUnitRate: number | null;
  totalQuantitySold: number;
  totalRevenue: number;
  invoiceCount: number;
  lastSoldDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Category {
  category: string;
  productCount: number;
  totalRevenue: number;
}

export interface ProductListResponse {
  products: Product[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
