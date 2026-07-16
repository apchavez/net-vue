import { test, expect } from "@playwright/test";

const PRODUCT = {
  id: 1,
  sku: "SKU-E2E-001",
  name: "E2E Test Product",
  description: "A product used in e2e tests",
  category: "Testing",
  price: 9.99,
  stock: 10,
  active: true,
};

async function loginAndMock(page: import("@playwright/test").Page) {
  await page.addInitScript(
    ({ token, username, roles }) => {
      window.localStorage.setItem("auth_token", token);
      window.localStorage.setItem("auth_username", username);
      window.localStorage.setItem("auth_roles", JSON.stringify(roles));
    },
    { token: "fake-jwt", username: "admin", roles: ["ADMIN", "USER"] },
  );

  await page.route("**/api/v1/products/active*", (route) =>
    route.fulfill({
      json: {
        content: [PRODUCT],
        page: 0,
        size: 20,
        totalElements: 1,
        totalPages: 1,
        last: true,
      },
    }),
  );
}

test("shows the products list", async ({ page }) => {
  await loginAndMock(page);
  await page.goto("/products");
  await expect(page.getByText(PRODUCT.name)).toBeVisible();
});

test("shows inactive products when the toggle is checked", async ({ page }) => {
  const INACTIVE_PRODUCT = {
    ...PRODUCT,
    id: 2,
    sku: "SKU-E2E-002",
    name: "Deactivated Product",
    active: false,
  };

  await loginAndMock(page);
  await page.route("**/api/v1/products/inactive*", (route) =>
    route.fulfill({
      json: {
        content: [INACTIVE_PRODUCT],
        page: 0,
        size: 20,
        totalElements: 1,
        totalPages: 1,
        last: true,
      },
    }),
  );

  await page.goto("/products");
  await expect(page.getByText(PRODUCT.name)).toBeVisible();

  await page.getByTestId("show-inactive").locator("input").check();

  await expect(page.getByText(INACTIVE_PRODUCT.name)).toBeVisible();
  await expect(page.getByText(PRODUCT.name)).toHaveCount(0);
});

test("navigates to the new product form", async ({ page }) => {
  await loginAndMock(page);
  await page.goto("/products");
  await page.getByTestId("new-product").click();
  await expect(page).toHaveURL(/\/products\/new/);
});

test("creates a product", async ({ page }) => {
  await loginAndMock(page);
  await page.route("**/api/v1/products", (route) => {
    if (route.request().method() === "POST") {
      return route.fulfill({ status: 201, json: PRODUCT });
    }
    return route.continue();
  });

  await page.goto("/products/new");
  await page.getByTestId("sku").locator("input").fill("SKU-NEW");
  await page.getByTestId("name").locator("input").fill("New Product");
  await page.getByTestId("category").locator("input").fill("Testing");
  await page.getByTestId("price").locator("input").fill("19.99");
  await page.getByTestId("stock").locator("input").fill("5");
  await page.getByTestId("submit").click();
  await expect(page).toHaveURL(/\/products$/);
});

test("deletes a product", async ({ page }) => {
  let deleted = false;

  await page.addInitScript(
    ({ token, username, roles }) => {
      window.localStorage.setItem("auth_token", token);
      window.localStorage.setItem("auth_username", username);
      window.localStorage.setItem("auth_roles", JSON.stringify(roles));
    },
    { token: "fake-jwt", username: "admin", roles: ["ADMIN", "USER"] },
  );

  await page.route("**/api/v1/products/active*", (route) =>
    route.fulfill({
      json: {
        content: deleted ? [] : [PRODUCT],
        page: 0,
        size: 20,
        totalElements: deleted ? 0 : 1,
        totalPages: 1,
        last: true,
      },
    }),
  );

  await page.route(`**/api/v1/products/${PRODUCT.id}`, (route) => {
    if (route.request().method() === "DELETE") {
      deleted = true;
      return route.fulfill({ status: 204 });
    }
    return route.continue();
  });

  await page.goto("/products");
  await page.getByTestId("delete-btn").first().click();
  await expect(page.getByText(PRODUCT.name)).toHaveCount(0);
});
