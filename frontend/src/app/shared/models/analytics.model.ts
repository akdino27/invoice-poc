export interface ProductSales {
  productId: string;
  productName: string;
  category: string | null;
  totalQuantity: number;
  totalRevenue: number;
  invoiceCount: number;
  averageUnitRate: number;
}

export interface ProductTrend {
  productId: string;
  productName: string;
  category: string | null;
  totalQuantity: number;
  totalRevenue: number;
  invoiceCount: number;
  growthRate: number;
  rank: number;
}

export interface CategorySales {
  category: string;
  productCount: number;
  totalQuantity: number;
  totalRevenue: number;
  invoiceCount: number;
  averageOrderValue: number;
}
