# API — ASP.NET Core Web API

Backend del cuarto hermano del dominio de Gestión de Productos ([quarkus-react](https://github.com/apchavez/quarkus-react), [spring-webflux-angular](https://github.com/apchavez/spring-webflux-angular), [spring-mvc-angular](https://github.com/apchavez/spring-mvc-angular)). C# / .NET 9, ASP.NET Core Web API, Clean/Hexagonal Architecture, mismo dominio (`sku`/`name`/`description`/`category`/`price`/`stock`/`active`) y mismos 11 endpoints REST que sus hermanos, incluyendo importación masiva por CSV y reportes descargables en PDF/Excel.

---

## Estructura

```
api/src
├── ProductApi.Domain          Entidad Product, excepciones de dominio, eventos, puertos (interfaces)
├── ProductApi.Application     ProductService — orquestación de casos de uso, publicación de eventos
├── ProductApi.Infrastructure  ProductRepository (EF Core/Npgsql), CachedProductRepository (Redis
│                               cache-aside), KafkaProductEventPublisher, RateLimitingMiddleware,
│                               JwtTokenService, DemoUserStore
└── ProductApi.Api             Controllers, DTOs, middleware de excepciones, composition root (Program.cs)
```

**Regla de dependencias:** `Api` → `Infrastructure` → `Application` → `Domain`. El dominio no conoce las capas externas — mismo principio hexagonal que los 3 hermanos, expresado con las herramientas nativas de .NET (interfaces `IProductRepository`/`IProductEventPublisher` en `Domain.Ports`, inyección de dependencias en `Program.cs` como composition root en vez de un framework de DI declarativo).

---

## Endpoints

Ruta base: `/api/v1/products`. Todos los endpoints excepto `/api/v1/auth/login`, `/health*` y `/metrics` requieren un JWT Bearer (`[Authorize]`); los endpoints de escritura (`POST`/`PUT`/`DELETE`) requieren el rol `ADMIN`.

| Método | Ruta | Descripción | Respuestas |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Login contra un store de usuarios demo (`admin`/`admin123` → ADMIN+USER, `user`/`user123` → USER), retorna un JWT RS256 | `200`, `401` |
| `POST` | `/api/v1/products` | Crear producto | `201`, `400`, `409` (SKU duplicado), `422` (regla de dominio) |
| `GET` | `/api/v1/products/active?page=&size=` | Listado paginado de productos activos (cacheado, cache-aside) | `200` |
| `GET` | `/api/v1/products/inactive?page=&size=` | Listado paginado de productos desactivados (vista admin, sin caché — bajo tráfico) | `200` |
| `GET` | `/api/v1/products/search?prefix=&page=&size=` | Búsqueda por prefijo de nombre, sin distinguir mayúsculas/minúsculas, paginada | `200` |
| `GET` | `/api/v1/products/sku/{sku}` | Búsqueda exacta por SKU | `200`, `404` |
| `GET` | `/api/v1/products/{id}` | Búsqueda por ID | `200`, `404` |
| `PUT` | `/api/v1/products/{id}` | Actualización completa | `200`, `400`, `404`, `422` |
| `DELETE` | `/api/v1/products/{id}` | Eliminar producto | `204`, `404` |
| `POST` | `/api/v1/products/import` | Importación masiva vía CSV (multipart, campo `file`) | `200` (siempre — ver sección de abajo) |
| `GET` | `/api/v1/products/report/pdf` | Descarga un reporte PDF de todos los productos | `200` |
| `GET` | `/api/v1/products/report/excel` | Descarga un reporte Excel (.xlsx) de todos los productos | `200` |

Swagger UI está disponible en `/swagger` en el entorno Development.

### Importación y reportes

- **`POST /api/v1/products/import`** (rol `ADMIN`, igual que crear un producto individual): sube un CSV multipart (campo `file`) con encabezado `sku,name,description,category,price,stock,active`. Cada fila se valida y persiste a través del mismo `IProductService.CreateProductAsync` que usa el endpoint de creación individual — no duplica la lógica de validación. Las filas inválidas (columnas incorrectas, precio/stock/booleano mal formado, SKU duplicado o inválido) se omiten y se reportan, sin abortar el resto del archivo. Respuesta: `{"imported": <n>, "failed": <n>, "errors": [{"row": <n>, "message": "..."}]}`.
- **`GET /api/v1/products/report/pdf`** y **`GET /api/v1/products/report/excel`** (roles `ADMIN`/`USER`, igual que los listados): generan un reporte con todos los productos (SKU, nombre, categoría, precio, stock, activo) más un resumen de conteo total y valor de inventario (`Σ precio × stock`). PDF vía [QuestPDF](https://www.questpdf.com/) (licencia Community — gratuita para organizaciones con menos de $1M USD de ingresos anuales, aplicable a un proyecto de portafolio), Excel vía [ClosedXML](https://github.com/ClosedXML/ClosedXML).

### Health checks

Igual que los 3 hermanos (SmallRye Health en Quarkus, Actuator en los dos Spring), expuestos vía `Microsoft.Extensions.Diagnostics.HealthChecks` nativo de ASP.NET Core:

| Ruta | Uso | Verifica |
|---|---|---|
| `GET /health` | Vista completa (todos los checks) | Liveness + Readiness |
| `GET /health/live` | Liveness probe de K8s | Solo que el proceso está vivo (check `self`, siempre `Healthy`) |
| `GET /health/ready` | Readiness probe de K8s | Conectividad real a PostgreSQL (`AddDbContextCheck<ProductDbContext>`) — si la base no responde, el pod deja de recibir tráfico |

### Eventos de dominio

`ProductCreated`/`ProductUpdated`/`ProductDeleted` se publican al topic de Kafka `product-events` en cada creación/actualización/eliminación vía `KafkaProductEventPublisher` (Confluent.Kafka) — fire-and-forget: un fallo al publicar nunca hace fallar la petición HTTP que lo originó (misma filosofía que los hermanos Quarkus/Spring).

### Caché de lectura

`GET /api/v1/products/active` se cachea en Redis mediante un decorador cache-aside (`CachedProductRepository`) — TTL de 5 minutos, clave por página/tamaño, invalidado en cada creación/actualización/eliminación mediante un contador de versión. Si Redis no está disponible, las lecturas/escrituras a la caché fallan de forma abierta (fail-open) — registra una advertencia y pasa directo a PostgreSQL.

### Rate limiting

`POST`/`PUT`/`DELETE` bajo `/api/v1/products` están limitados a **100 peticiones por ventana fija de 60 segundos por IP de cliente** (`RateLimitingMiddleware`), aplicado mediante un script Lua atómico en Redis (`INCR` + `EXPIRE` condicional) — mismo algoritmo y mismos números que los hermanos Quarkus/Spring. Exceder el límite retorna `429 Too Many Requests` con header `Retry-After: 60`. Si Redis no está disponible, el middleware falla de forma abierta.

---

## Seguridad

JWT **RS256** (`Microsoft.AspNetCore.Authentication.JwtBearer`). `JwtTokenService` firma con una clave RSA-2048: en memoria por defecto (dev/test/CI — un solo proceso firma y verifica), o desde `Jwt:PrivateKeyPem`/`Jwt:PrivateKeyPath` en un despliegue real multi-réplica (ver README raíz, sección de Kubernetes).

| Ruta | Rol requerido |
|---|---|
| `/api/v1/auth/login` | Público |
| `/api/v1/products/**` (`GET`) | Cualquier usuario autenticado (`USER` o `ADMIN`) |
| `/api/v1/products/**` (`POST`/`PUT`/`DELETE`) | Solo `ADMIN` |
| `/health*`, `/metrics`, `/swagger*` | Público (anónimo) |

La colección de Postman incluye una request de login que captura el token automáticamente y lo reutiliza vía la auth Bearer a nivel de colección — ejecutarla primero antes de cualquier petición protegida.

---

## Desarrollo local

```bash
docker compose up -d          # Postgres, Redis, Kafka, Prometheus, Grafana (desde la raíz del repo)
cd api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=productdb;Username=product_user;Password=product_pass" --project src/ProductApi.Api
dotnet run --project src/ProductApi.Api
```

---

## Testing

```bash
cd api
dotnet test tests/ProductApi.UnitTests
dotnet test tests/ProductApi.IntegrationTests   # respaldado por Testcontainers con Postgres
```

| Tipo | Clase | Casos | Cubre |
|---|---|---|---|
| Unitarias | `ProductTests` | 10 | Invariantes de la entidad `Product` |
| Unitarias | `ProductServiceTests` | 14 | Orquestación de casos de uso, publicación de eventos |
| Unitarias | `CachedProductRepositoryTests` | 9 | Cache-aside: lectura/escritura/invalidación, fail-open sin Redis |
| Unitarias | `JwtTokenServiceTests` | 2 | Firma/verificación de tokens RS256 |
| Unitarias | `DemoUserStoreTests` | 4 | Validación de credenciales demo |
| Unitarias | `ProductCsvParserTests` | 8 | Parseo de CSV: filas válidas, comillas con comas, columnas/valores inválidos |
| Unitarias | `ProductReportGeneratorTests` | 2 | Generación de PDF (cabecera `%PDF`) y Excel (valores de celda) |
| Integración (Testcontainers + WebApplicationFactory) | `ProductsControllerIntegrationTests` | 19 | Todos los endpoints y códigos de respuesta reales contra Postgres, incluyendo import CSV y reportes PDF/Excel |

**49 tests unitarios + 19 de integración = 68 tests de backend** (conteo real de `[Fact]`/`[Theory]`, no estimado).

---

## Proyectos Relacionados

Ver la tabla completa en el [README raíz](../README.md#proyectos-relacionados).
