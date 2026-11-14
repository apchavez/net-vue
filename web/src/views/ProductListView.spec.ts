import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createRouter, createMemoryHistory } from "vue-router";
import vuetify from "../plugins/vuetify";
import { useAuthStore } from "../stores/auth";
import ProductListView from "./ProductListView.vue";

const ACTIVE_PRODUCT = {
  id: 1,
  sku: "SKU-ACTIVE",
  name: "Active Widget",
  description: null,
  category: null,
  price: 9.99,
  stock: 10,
  active: true,
};

const INACTIVE_PRODUCT = {
  id: 2,
  sku: "SKU-INACTIVE",
  name: "Inactive Widget",
  description: null,
  category: null,
  price: 4.99,
  stock: 0,
  active: false,
};

function page(items: unknown[]) {
  return {
    content: items,
    page: 0,
    size: 20,
    totalElements: items.length,
    totalPages: 1,
    last: true,
  };
}

vi.mock("../api/productsApi", () => ({
  listActive: vi.fn(),
  listInactive: vi.fn(),
  searchByPrefix: vi.fn(),
  remove: vi.fn(),
}));

import { listActive, listInactive } from "../api/productsApi";

describe("ProductListView", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    useAuthStore().setSession("tok", "admin", ["ADMIN", "USER"]);
    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: "/products", component: ProductListView },
        { path: "/products/new", component: { template: "<div />" } },
      ],
    });
    vi.clearAllMocks();
    vi.mocked(listActive).mockResolvedValue({
      data: page([ACTIVE_PRODUCT]),
    } as never);
    vi.mocked(listInactive).mockResolvedValue({
      data: page([INACTIVE_PRODUCT]),
    } as never);
  });

  it("loads active products by default", async () => {
    const wrapper = mount(ProductListView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    expect(listActive).toHaveBeenCalledTimes(1);
    expect(listInactive).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("Active Widget");
  });

  it("switches to inactive products when the toggle is checked", async () => {
    const wrapper = mount(ProductListView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    const checkbox = wrapper.find('[data-testid="show-inactive"] input');
    await checkbox.setValue(true);
    await flushPromises();

    expect(listInactive).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain("Inactive Widget");
  });
});

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0));
}
