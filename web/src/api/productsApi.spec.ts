import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("./httpClient", () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import httpClient from "./httpClient";
import {
  listActive,
  listInactive,
  searchByPrefix,
  findBySku,
  getById,
  create,
  update,
  remove,
} from "./productsApi";
import type { ProductRequest } from "../models/product";

const PRODUCT_REQUEST: ProductRequest = {
  sku: "SKU-001",
  name: "Widget",
  description: null,
  category: null,
  price: 9.99,
  stock: 5,
  active: true,
};

describe("productsApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("listActive calls GET /api/v1/products/active with pagination params", () => {
    listActive(2, 10);
    expect(httpClient.get).toHaveBeenCalledWith("/api/v1/products/active", {
      params: { page: 2, size: 10 },
    });
  });

  it("listActive defaults to page 0, size 20", () => {
    listActive();
    expect(httpClient.get).toHaveBeenCalledWith("/api/v1/products/active", {
      params: { page: 0, size: 20 },
    });
  });

  it("listInactive calls GET /api/v1/products/inactive with pagination params", () => {
    listInactive(1, 5);
    expect(httpClient.get).toHaveBeenCalledWith("/api/v1/products/inactive", {
      params: { page: 1, size: 5 },
    });
  });

  it("searchByPrefix calls GET /api/v1/products/search with the prefix", () => {
    searchByPrefix("wid", 0, 20);
    expect(httpClient.get).toHaveBeenCalledWith("/api/v1/products/search", {
      params: { prefix: "wid", page: 0, size: 20 },
    });
  });

  it("findBySku calls GET /api/v1/products/sku/:sku, URL-encoded", () => {
    findBySku("SKU/001");
    expect(httpClient.get).toHaveBeenCalledWith(
      "/api/v1/products/sku/SKU%2F001",
    );
  });

  it("getById calls GET /api/v1/products/:id", () => {
    getById(42);
    expect(httpClient.get).toHaveBeenCalledWith("/api/v1/products/42");
  });

  it("create calls POST /api/v1/products with the payload", () => {
    create(PRODUCT_REQUEST);
    expect(httpClient.post).toHaveBeenCalledWith(
      "/api/v1/products",
      PRODUCT_REQUEST,
    );
  });

  it("update calls PUT /api/v1/products/:id with the payload", () => {
    update(7, PRODUCT_REQUEST);
    expect(httpClient.put).toHaveBeenCalledWith(
      "/api/v1/products/7",
      PRODUCT_REQUEST,
    );
  });

  it("remove calls DELETE /api/v1/products/:id", () => {
    remove(7);
    expect(httpClient.delete).toHaveBeenCalledWith("/api/v1/products/7");
  });
});
