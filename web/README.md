# Web — Vue 3

Frontend del cuarto hermano del dominio de Gestión de Productos ([quarkus-react](https://github.com/apchavez/quarkus-react) usa React, [spring-webflux-angular](https://github.com/apchavez/spring-webflux-angular) y [spring-mvc-angular](https://github.com/apchavez/spring-mvc-angular) comparten el mismo Angular) — Vue 3 (Composition API, `<script setup>`), Vuetify (Material Design), Pinia para estado global, Vue Router, Axios.

---

## Estructura

```
web/src
├── api            authApi.ts, productsApi.ts (llamadas HTTP vía httpClient.ts), httpClient.ts
│                  (Axios con interceptor que agrega el header Authorization y redirige a
│                  /login en un 401)
├── stores         auth.ts — store Pinia con el JWT, usuario y roles actuales (persistido en
│                  localStorage)
├── views          LoginView.vue, ProductListView.vue, ProductFormView.vue (crear/editar)
├── router         rutas + guard `requiresAuth` que redirige a /login si no hay sesión
├── components     componentes Vuetify reutilizables
├── models         tipos TypeScript compartidos (Product, LoginRequest, etc.)
└── plugins        registro de Vuetify
```

---

## Funcionalidades

- **Login** (`/login`) contra `POST /api/v1/auth/login` — guarda el JWT y los roles en el store `auth` (Pinia), persistido en `localStorage` para sobrevivir un refresh.
- **Listado de productos** (`/products`, ruta protegida) — tabla paginada con búsqueda por prefijo, respaldada por `GET /api/v1/products/active`.
- **Crear/editar producto** (`/products/new`, `/products/:id/edit`, rutas protegidas) — formulario Vuetify con validación, botones de escritura visibles solo para el rol `ADMIN`.
- **Logout** — limpia el store y redirige a `/login`.
- **Guard de rutas**: `router.beforeEach` redirige a `/login` cualquier ruta con `meta.requiresAuth` si no hay sesión activa; el interceptor de Axios en `httpClient.ts` hace lo mismo ante cualquier `401` recibido del backend (sesión expirada).

---

## Desarrollo local

```bash
npm install
npm run dev          # http://localhost:5173, contra la API en http://localhost:8080
```

## Testing

```bash
npm run test          # Vitest + Vue Test Utils
npm run test:e2e      # Playwright (API real vía docker compose, o mockeada según el spec)
```

| Tipo             | Archivo                             | Casos                                           |
| ---------------- | ----------------------------------- | ----------------------------------------------- |
| Unitarias        | `src/stores/auth.spec.ts`           | 4 — login/logout, persistencia, roles           |
| Unitarias        | `src/views/LoginView.spec.ts`       | 2 — submit exitoso, credenciales inválidas      |
| Unitarias        | `src/views/ProductListView.spec.ts` | 2 — render de la tabla, búsqueda                |
| E2E (Playwright) | `e2e/products.spec.ts`              | 5 — flujo completo de login + CRUD de productos |

**8 tests unitarios + 5 E2E = 13 tests de frontend** (conteo real de `it(`/`test(`, no estimado).

## Build de producción

```bash
npm run build    # vue-tsc -b && vite build → dist/
npm run preview  # sirve dist/ localmente para verificar el build
```

---

## Proyectos Relacionados

Ver la tabla completa en el [README raíz](../README.md#proyectos-relacionados).
