export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  category: string | null;
  price: number;
  stock: number;
  active: boolean;
}

export interface ProductRequest {
  sku: string;
  name: string;
  description: string | null;
  category: string | null;
  price: number;
  stock: number;
  active: boolean;
}

export interface PageResponse<T> {
  content: T[];
  page: number;
  size: number;
  totalElements: number;
  totalPages: number;
  last: boolean;
}
