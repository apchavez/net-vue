import httpClient from "./httpClient";
import type { PageResponse, Product, ProductRequest } from "../models/product";

export function listActive(page = 0, size = 20) {
  return httpClient.get<PageResponse<Product>>("/api/v1/products/active", {
    params: { page, size },
  });
}

export function listInactive(page = 0, size = 20) {
  return httpClient.get<PageResponse<Product>>("/api/v1/products/inactive", {
    params: { page, size },
  });
}

export function searchByPrefix(prefix: string, page = 0, size = 20) {
  return httpClient.get<PageResponse<Product>>("/api/v1/products/search", {
    params: { prefix, page, size },
  });
}

export function findBySku(sku: string) {
  return httpClient.get<Product>(
    `/api/v1/products/sku/${encodeURIComponent(sku)}`,
  );
}

export function getById(id: number) {
  return httpClient.get<Product>(`/api/v1/products/${id}`);
}

export function create(product: ProductRequest) {
  return httpClient.post<Product>("/api/v1/products", product);
}

export function update(id: number, product: ProductRequest) {
  return httpClient.put<Product>(`/api/v1/products/${id}`, product);
}

export function remove(id: number) {
  return httpClient.delete(`/api/v1/products/${id}`);
}
