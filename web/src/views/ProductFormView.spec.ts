import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createRouter, createMemoryHistory } from "vue-router";
import vuetify from "../plugins/vuetify";
import ProductFormView from "./ProductFormView.vue";

const EXISTING_PRODUCT = {
  id: 5,
  sku: "SKU-005",
  name: "Existing Widget",
  description: "desc",
  category: "cat",
  price: 12.5,
  stock: 3,
  active: false,
};

vi.mock("../api/productsApi", () => ({
  getById: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
}));

import { getById, create, update } from "../api/productsApi";

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe("ProductFormView", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    setActivePinia(createPinia());
    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: "/products", component: { template: "<div />" } },
        { path: "/products/new", component: ProductFormView },
        {
          path: "/products/:id/edit",
          component: ProductFormView,
          props: true,
        },
      ],
    });
    vi.clearAllMocks();
    vi.mocked(create).mockResolvedValue({ data: {} } as never);
    vi.mocked(update).mockResolvedValue({ data: {} } as never);
  });

  it("creates a product with the entered fields and navigates back to the list", async () => {
    await router.push("/products/new");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    await wrapper.find('[data-testid="sku"] input').setValue("SKU-NEW");
    await wrapper.find('[data-testid="name"] input').setValue("New Widget");
    await wrapper
      .find('[data-testid="category"] input')
      .setValue("Electronics");
    await wrapper.find('[data-testid="price"] input').setValue("19.99");
    await wrapper.find('[data-testid="stock"] input').setValue("7");

    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();

    expect(create).toHaveBeenCalledWith({
      sku: "SKU-NEW",
      name: "New Widget",
      description: null,
      category: "Electronics",
      price: 19.99,
      stock: 7,
      active: true,
    });
    expect(update).not.toHaveBeenCalled();
    expect(router.currentRoute.value.path).toBe("/products");
  });

  it("preloads the existing product in edit mode and updates it", async () => {
    vi.mocked(getById).mockResolvedValue({ data: EXISTING_PRODUCT } as never);

    await router.push("/products/5/edit");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    expect(getById).toHaveBeenCalledWith(5);
    expect(
      (wrapper.find('[data-testid="sku"] input').element as HTMLInputElement)
        .value,
    ).toBe("SKU-005");
    expect(
      (wrapper.find('[data-testid="name"] input').element as HTMLInputElement)
        .value,
    ).toBe("Existing Widget");

    await wrapper.find('[data-testid="name"] input').setValue("Renamed Widget");
    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();

    expect(update).toHaveBeenCalledWith(5, {
      sku: "SKU-005",
      name: "Renamed Widget",
      description: "desc",
      category: "cat",
      price: 12.5,
      stock: 3,
      active: false,
    });
    expect(create).not.toHaveBeenCalled();
    expect(router.currentRoute.value.path).toBe("/products");
  });

  it("the SKU field is disabled in edit mode", async () => {
    vi.mocked(getById).mockResolvedValue({ data: EXISTING_PRODUCT } as never);

    await router.push("/products/5/edit");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    expect(
      (wrapper.find('[data-testid="sku"] input').element as HTMLInputElement)
        .disabled,
    ).toBe(true);
  });

  it("cancel navigates back to the list without submitting", async () => {
    await router.push("/products/new");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    const cancelButton = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Cancel"))!;
    await cancelButton.trigger("click");
    await flushPromises();

    expect(create).not.toHaveBeenCalled();
    expect(router.currentRoute.value.path).toBe("/products");
  });

  it("shows validation errors and does not submit when required fields are empty", async () => {
    await router.push("/products/new");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();

    expect(wrapper.text()).toContain("El SKU es requerido");
    expect(wrapper.text()).toContain("El nombre es requerido");
    expect(wrapper.text()).toContain("La categoría es requerida");
    expect(create).not.toHaveBeenCalled();
  });

  it("shows a validation error for an invalid SKU format", async () => {
    await router.push("/products/new");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    await wrapper.find('[data-testid="sku"] input').setValue("bad sku!");
    await wrapper.find('[data-testid="name"] input').setValue("New Widget");
    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();

    expect(wrapper.text()).toContain(
      "Solo letras, números, guiones y guiones bajos (máx. 50)",
    );
    expect(create).not.toHaveBeenCalled();
  });

  it("shows a validation error for a negative price", async () => {
    await router.push("/products/new");
    const wrapper = mount(ProductFormView, {
      global: { plugins: [router, vuetify] },
    });
    await flushPromises();

    await wrapper.find('[data-testid="sku"] input').setValue("SKU-NEW");
    await wrapper.find('[data-testid="name"] input').setValue("New Widget");
    await wrapper.find('[data-testid="price"] input').setValue("-5");
    await wrapper.find("form").trigger("submit.prevent");
    await flushPromises();

    expect(wrapper.text()).toContain("El precio debe ser mayor o igual a 0");
    expect(create).not.toHaveBeenCalled();
  });
});
