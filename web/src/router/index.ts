import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "../stores/auth";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", redirect: "/products" },
    {
      path: "/login",
      name: "login",
      component: () => import("../views/LoginView.vue"),
    },
    {
      path: "/products",
      name: "products",
      component: () => import("../views/ProductListView.vue"),
      meta: { requiresAuth: true },
    },
    {
      path: "/products/new",
      name: "product-new",
      component: () => import("../views/ProductFormView.vue"),
      meta: { requiresAuth: true },
    },
    {
      path: "/products/:id/edit",
      name: "product-edit",
      component: () => import("../views/ProductFormView.vue"),
      meta: { requiresAuth: true },
      props: true,
    },
    { path: "/:pathMatch(.*)*", redirect: "/products" },
  ],
});

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: "login" };
  }
});

export default router;
