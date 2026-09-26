# Fases de Mejora — TiendaDawApi CQRS/MediatR (optimización + calidad)

> **Proyecto:** `TiendaDawApi-Cqrs-MediatR-NetCore` (CQRS + MediatR)  
> **Rama:** `feature/polly`  
> **Proyecto origen:** `TiendaDawApi-NetCore` — fases 0-12 **completadas allí** (ver su `FASES-MEJORAS.md` y esta misma `BITACORA.md`, con el checklist "Replicar en CQRS" por fase)  
> **Estado:** 🟡 **en curso — Fase 0 y 5 COMPLETADAS** — Fase 13 y 14 pendientes, fases 1-12 pendientes de replicar.  
> **Orden de ejecución:** Fase 0 → Fase 5 → Fase 13 → **Fase 14 (Paridad)** → Fase 1 → Fase 2 → Fase 3 → Fase 4 → Fase 9 → Fase 8 → Fase 7 → Fase 11 → Fase 6 → Fase 10 → Fase 12  
> **Regla de oro:** no romper nada de lo existente (E2E Bruno/Newman, tests unitarios, flujos de pedidos). El cliente no debe saber si usa el proyecto A o el B.

---

## Decisiones cerradas

| Decisión | Acuerdo |
|----------|---------|
| Efectos secundarios (email, WS, SignalR, cache) | **Fire & forget** con `_ = Task.Run` — **NO** `await Task.WhenAll` (el email no debe bloquear la respuesta HTTP) |
| Calidad del fire & forget | Sí endurecer: `try/catch + LogError` en el interior de cada `Task.Run` (~30 sitios) |
| `AsNoTracking` | Selectivo en **solo lectura**; **no tocar** `FindByIdAsync` (lo comparten Update/Delete/soft-delete) |
| Caché HTTP | **Opción A: OutputCache + ETag + 304** (no ResponseCaching en los mismos endpoints) |
| Polly | Solo **fase educativa** aditiva (email); no hay `HttpClient` saliente real en la API |
| Migraciones | Sí: salir de `EnsureCreated` puro para que **los índices se apliquen en BD existente** |
| Integración Result→HTTP | **Opción C: extensión `ToHttpResult()`** (doc UD02 §7) — 31 `error switch` → 1 extensión; **preservar códigos actuales** |
| Queries de productos (Fase 13) | **Escrituras → PostgreSQL · lecturas → MongoDB** (colección `productos_read` desnormalizada, mínimo `categoria.nombre`), sync por **Domain Events + MediatR** (apuntes §30.10); **categorías quedan 100% en PostgreSQL** |

---

## Fase 0 — Baseline ✅ COMPLETADA (25/09/2026)

| # | Tarea | Verificación |
|---|-------|--------------|
| 0.1 | `dotnet build` + unit tests como referencia | ✅ Build **0 errores 0 warnings** (`TreatWarningsAsErrors`); baseline propio **854 unit + 94 integración** verdes (antes y después de los saltos; el origen: 1034/161) |
| 0.2 | Fix `NU1902` SharpCompress 0.30.1 (CVE zip-slip, transitivo de MongoDB.Driver 3.6.0) | ✅ Paquetes MongoDB actualizados → SharpCompress 0.48.1 |
| 0.3 | Actualización general de dependencias a últimas versiones estables | ✅ `--vulnerable` → **0**; `--outdated` → solo las 3 excepciones de abajo (ver tabla) |

### Actualización de paquetes (0.3)

**Api:** AutoMapper 16.2.0 · BCrypt 4.2.0 · CSharpFunctionalExtensions 3.7.0 · FluentValidation 11.3.1 · **HotChocolate 16.6.7** · MailKit 4.18.0 · JwtBearer 10.0.12 · EF Core 10.0.12 · MongoDB.Bson 3.12.0 · Npgsql 10.0.3 · Serilog 10.0.0 · StackExchange.Redis 3.3.1 · **Swashbuckle 10.2.3** · System.IdentityModel 8.23.0 · **`Microsoft.AspNetCore.Mvc.Versioning` (deprecated) → `Asp.Versioning.Mvc` 10.2.1 + ApiExplorer**

**Tests:** coverlet 10.0.1 · FluentAssertions 7.2.2 · Mvc.Testing 10.0.12 · Test.Sdk 18.10.1 · Moq 4.21.0 · NUnit 4.6.1 · NUnit3TestAdapter 6.3.0 · Testcontainers 4.15.0 · MongoDB.Driver 3.12.0

**Adaptaciones de código por saltos major:**
- `SwaggerConfig.cs`: Microsoft.OpenApi 2.x (tipos en raíz, `OpenApiSecuritySchemeReference`, `AddSecurityRequirement(Func<OpenApiDocument,…>)`)
- `ApiVersioningConfig.cs`: cadena `.AddApiVersioning(…).AddApiExplorer().AddMvc()` (analizadores AV0013/AV0021)
- Tests: constructores de Testcontainers con imagen vía helper compartido `TestContainerImages.cs` (`new MongoDbBuilder(TestContainerImages.Mongo)` = `mongo:7.0` / `postgres:17-alpine`, alineado con los composes), `v!` en matchers Moq 4.21, `HotChocolateCompositeImplicitUsings=disable` (colisión `Is` con NUnit)
- `AuthController.cs` / `ControllersConfig.cs`: usings por el cambio de paquete de versionado (`using Asp.Versioning;` / retirar el deprecatado)

**Excepciones intencionadas (no actualizar):**
- `AutoMapper.Extensions.MS.DI 12.0.0` → 12.0.1 exige `AutoMapper = 12.0.1` exacto (rompería el 16.2.0)
- `FluentAssertions 7.2.2` → v8 cambió a licencia Xceed (solo gratis no-comercial); rama 7 = Apache 2.0
- `MediatR 12.5.0` → 13+/14.x es licencia comercial (Lucky Penny, RPL1.5); 12.5.0 = última libre Apache 2.0 (propio de este repo, el origen no usa MediatR)

**Resultado ejecutado (25/09/2026):** `restore` OK · `build` **0/0** · unit **854/854** (6 s) · integración **94/94** (38 s, Testcontainers) · `--vulnerable` **0** · `--outdated` **3 excepciones** (las de arriba).

---

## Fase 1 — Rápido y de bajo riesgo ⬜ PENDIENTE (replicar)

### 1A · Health Checks (#10)

| # | Tarea | Archivos |
|---|-------|----------|
| 10.1 | `Infrastructures/HealthChecksConfig.cs`: `AddHealthChecks()` con PG (`CanConnectAsync`), Mongo (ping); Redis si prod. **Sin paquetes NuGet** (checks propios) | nuevo |
| 10.2 | `MapHealthChecks("/health", ...)` con JSON (`status`, `checks[]` con `name`, `status`, `duration`) | `HealthChecksConfig.cs` |
| 10.3 | Registrar en `Program.cs` (servicios + endpoint) | `Program.cs` |
| 10.4 | Verificar | `GET /health` → 200; BD caída → 503 |

> El Bruno `[001] Health Check` y el Automation ya esperan `GET /health`.

### 1B · Índices EF en el modelo (#2)

| # | Tarea | Archivos |
|---|-------|----------|
| 2.1 | `Producto`: `HasIndex(CategoriaId)`, `HasIndex(CreatedAt)`, `HasIndex(IsDeleted)`, compuesto `(CategoriaId, Precio)` | `Data/TiendaDbContext.cs` |
| 2.2 | `User`: `HasIndex(Role)` | `Data/TiendaDbContext.cs` |
| 2.3 | **Solo añadir**, no quitar índices existentes (categorías/users únicos) | — |
| 2.4 | Aplicación en BD viva → **Fase 8 (migraciones)**; en dev con drop+create bastan | — |

### 1C · Endurecer `Task.Run` (FF)

| # | Tarea | Archivos |
|---|-------|----------|
| FF.1 | Inventario de ~30 `_ = Task.Run(...)` | grep |
| FF.2 | Interior con `try { ... } catch (Exception ex) { logger.LogError(ex, "..."); }` — **sin await, sin WhenAll** | `ProductoService` (~13), `PedidosService` (~11), `CategoriaService` (2), `UserService` (3) |
| FF.3 | Verificar | Build; crear producto → HTTP rápido + logs sin excepciones en background |

### 📋 Verificación Fase 1 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 2 — Consultas (`AsNoTracking`) ⬜ PENDIENTE (replicar)

| # | Tarea | Detalle |
|---|-------|---------|
| 1.1 | Sí | `FindAllAsync`, `FindAllPagedAsync`, `FindByCategoriaIdAsync`, `GetRecentlyCreatedAsync`, listados de `User` |
| 1.2 | **No tocar** | `FindByIdAsync`, `DeleteAsync` (soft-delete depende de tracking), rutas de `Update` |
| 1.3 | Verificar | GETs OK; **PUT/DELETE** producto/categoría/user OK (E2E) |

### 📋 Verificación Fase 2 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 3 — Paginación real de pedidos (#4) ⬜ PENDIENTE (replicar)

| # | Tarea | Archivos |
|---|-------|----------|
| 4.1 | `FindAllPagedAsync(page, size)` → `(Items, TotalCount)` | `IPedidosRepository.cs` |
| 4.2 | Mongo: `Skip/Limit` + `CountDocuments` | `PedidosNativeRepository.cs` |
| 4.3 | EF: `Skip/Take` + `CountAsync` | `PedidosEfCoreRepository.cs` |
| 4.4 | Servicio delega al repo; **borrar** paginación en memoria (`PedidosService.cs:65-69`) | `PedidosService.cs` |
| 4.5 | Misma firma de servicio → controller intacto | — |
| 4.6 | Verificar | `GET /api/pedidos/paged` devuelve solo `size` |

### 📋 Verificación Fase 3 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 4 — Caché HTTP · **Opción A** (OutputCache + ETag) ⬜ PENDIENTE (replicar)

| # | Tarea | Archivos |
|---|-------|----------|
| 6.1 | `AddOutputCache`: política 60s + tags `productos`/`categorias` | `Infrastructures/OutputCacheConfig.cs` + `Program.cs` |
| 6.2 | `app.UseOutputCache()` antes de `MapControllers` | `Program.cs` |
| 6.3 | `[OutputCache(...)]` **solo** GET anónimos de `ProductosController` y `CategoriasController` | 2 controllers |
| 6.4 | Invalidación por tag tras CUD (`IOutputCacheStore.EvictByTag`) | services/controllers |
| 6.5 | **Excluir** pedidos, users, auth, GraphQL autenticado | — |
| 6.6 | Verificar | 2º GET → **304**; tras POST/PUT → tag invalidado → 200 con cuerpo nuevo |

### 📋 Verificación Fase 4 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 5 — Verificación global ✅ COMPLETADA (25/09/2026)

| # | Tarea | Verificación |
|---|-------|--------------|
| 5.1 | `dotnet build` (warnings as errors) | ✅ Build **0 errores 0 warnings** |
| 5.2 | `dotnet test --filter "FullyQualifiedName~Unit"` | ✅ **855/855** (6 s) |
| 5.3 | Integration (Docker) | ✅ **94/94** (38 s, Testcontainers) |
| 5.4 | E2E Bruno/Newman: auth, productos C/R/U/D, pedidos paged, categorías | ✅ Newman **77 requests, 0 failed, 95/95 assertions** (2 esperados: `/health`); Bruno-Local **78 requests, 118/127 tests** (9 fallos: 2 health esperados + 7 expectativas de scripts); Automation **54/55** (`/health`) |
| 5.5 | Smoke: `/health`, `/swagger`, GraphQL, 2º GET → 304, logs Task.Run limpios | ✅ Swagger `/` 200, `/swagger/v1/swagger.json` 200, GraphQL 200, logs sin excepciones; `/health` pendiente (Fase 1), 304 pendiente (Fase 4) |
| 5.6 | **Automation Node** (Fase 7) en verde | ✅ **54/55** (solo `/health` esperado) |

### Bugs hallados y corregidos en esta fase

| Bug | Archivo | Fix |
|-----|---------|-----|
| Cache key de productos paginados no incluía filtros → `precioMax` devolvía resultados sin filtrar | `GetAllProductosQuery.cs` | Cache key = `$"productos:paged:{request.Filter}"` (usa `ToString()` del record) |
| PUT categoría no copiaba `Descripcion` (solo `Nombre`) | `UpdateCategoriaCommand.cs` | Añadido `categoria.Descripcion = request.Dto.Descripcion;` |
| Test unit no verificaba ambos campos en update | `UpdateCategoriaCommandHandlerTests.cs` | Nuevo test `Handle_ActualizaNombreYDescripcion_DevuelveSuccess` |

### Hallazgo operativo

La API necesita `ASPNETCORE_ENVIRONMENT=Development` al arrancar para ejecutar `EnsureDeleted + EnsureCreated + seed`. Sin él, `InitializeDatabaseAsync` solo hace `EnsureCreated()` (sin borrar ni sembrar), y la BD acumula datos de runs anteriores → sign-in de `userdaw` falla porque fue borrado/alterado por Newman. El archivo `start-api.bat` en temp incluye `set ASPNETCORE_ENVIRONMENT=Development`.

### 📋 Verificación Fase 5 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

### Arreglos aplicados a la colección `Postman-Cli` durante la verificación

La colección estaba **desactualizada/rota** respecto a la API actual; sin estos arreglos no podía ejecutarse (6 iteraciones de depuración):

| # | Problema | Arreglo |
|---|----------|---------|
| A | **JSON inválido** (3 items de categorías sin cerrar su objeto `request`) → `newman` ni siquiera parseaba | Cerrado el balance de llaves de los 3 items |
| B | **Variables de colección pisadas**: newman prioriza `environment` sobre `collectionVariables` → tokens/ids llegaban vacíos (401 en cadena) | `pm.collectionVariables.*` → `pm.environment.*` (20 sitios), un solo scope de variables |
| C | **Auth raíz heredada**: la colección declara `auth: Bearer {{adminToken}}` a nivel raíz → los tests "sin auth" recibían el token real y devolvían 201/200 | `"auth": {"type":"noauth"}` en `[016]`, `[043]`, `[058]`, `[067]` |
| D | **GraphQL obsoleto** (HotChocolate 16): `Int!` → `Long!` (`categoria`/`producto`), `crearProducto`→`createProducto`, `actualizarProducto`→`updateProducto`, `eliminarProducto`→`deleteProducto`, `ProductoInput`→`Create/UpdateProductoInput` | 8 queries/mutations reescritas al esquema actual (introspección `__schema`) |
| E | **Orden de ejecución**: `[035]` leía `pedidoId` antes de crearlo; la carpeta admin reutilizaba el pedido borrado por el usuario; `[053]` leía `testUserId` antes de `[055]`; `[051]` borraba `userdaw` antes de `[059]` | Reordenados: `[035]` tras `[036]`, `[051]` al final de la carpeta 6, **nuevo `[043b]`** (admin crea su propio pedido al entrar en la carpeta 5) |
| F | **Códigos esperados incorrectos**: ids inexistentes de pedido devuelven **404** (no 403) y `categoriaId` inexistente en producto devuelve **400** (validación), no 404 | Ajustados los asserts de `[039]`, `[041]`, `[028]` al comportamiento real de la API (verificado con curl) |
| G | `pm.response.status` es **string** en newman (rompía `[060]`) | `pm.response.code` |
| H | `--delay` no existe en newman 7 | `--delay-request` |

*Re-ejecución final (BD reiniciada con semilla): **4/4 tandas exit 0**, 0 fallos.*

### Arreglos aplicados a las colecciones Bruno (`Bruno-Local` + `Bruno-Cli`)

Ejecución: `bru run <carpetas> --env-file environments/… --delay 3200 -o results.json --format json` (CLI en temp; sin instalar globalmente). Carpetas fuera del run: `6 - USUARIOS` (**vacía**, sin requests) y `12 - WEBSOCKETS` (bru CLI no soporta WS).

| # | Problema | Arreglo |
|---|----------|---------|
| I | **Environment desactualizado**: `baseUrl:5000` (pisa la `5031` de `collection.bru`), `userUsername:user` (real: `userdaw`), passwords vacíos | Corregido en `Bruno-Local/environments/*.json`; `Bruno-Cli/local.bru` (Docker, `host.docker.internal`) se conserva y se pasa por `--env-var` si se ejecuta desde el host |
| J | **`graphqlProductoId` sin declarar** en `vars:pre-request` → quedaba `{{…}}` literal y `body:graphql:vars` de `[070]`/`[071]` no parseaba (`Expected property name…`) | Declarada con valor inicial `1` en `collection.bru` (ambas colecciones) |
| K | **Tests con shape/código antiguo**: `[003]` esperaba `message` (real: `errors` RFC 9457), `[011]`/`[034]` `totalItems` (real: **`totalCount`**), `[031]` `imagenUrl` (real: **`imagen`**), `[044]` `cliente`/`lineasPedido` (real: **`destinatario`**/**`items`**), `[063]`/`[065]`/`[066]` id `number` (real: **string**), `[067]`/`[068]` mensajes `Unauthorized`/`forbidden` (real: *"The current user is not authorized…"* → substring `authoriz`), `[069]` esperaba array `errors` (real: `data.createProducto: null` sin `errors`) | 10 tests ajustados al comportamiento verificado en vivo |
| L | **Códigos reales** (idéntico a Postman): `[028]` 404→**400**, `[039]`/`[041]` 403→**404** | Asserts corregidos |
| M | **Orden**: `[035]` (lee `pedidoId`) se ejecutaba antes de `[036]` (lo crea) | `seq` reordenados en la carpeta 4 |
| N | **Ejecución por tandas rompe los tokens** (`bru.setVar` vive solo en la sesión del proceso) | Corrida única de las 10 carpetas con `--delay 3200` (≤20 POST por ventana de 60 s) |

### 🐛 Hallazgo en la API (corregido en esta fase)

El test **`[019] PUT - Actualizar (Admin)`** de Bruno descubrió un bug real: `CategoriaService.UpdateAsync` solo copiaba `Nombre` y **ignoraba `dto.Descripcion`** → `200 OK` con la descripción antigua. Corregido (`categoria.Descripcion = dto.Descripcion;`) + test unit ampliado; verificado con build 0/0, unit 1034 y integration 161/0/32. Commits `1780ef6` (fix) y `8d1d5d9` (colecciones Bruno).

---

## Fase 6 — Polly educativa ⬜ PENDIENTE (replicar)

| # | Tarea | Detalle | Estado |
|---|-------|---------|--------|
| 6.1 | Paquetes | `Polly` **8.8.0** en `TiendaApi.csproj`. **`Microsoft.Extensions.Http.Polly` NO**: solo aporta policies para `HttpClient` y la API no tiene llamadas salientes reales (decisión coherente con "No aplica" más abajo) | ⬜ |
| 6.2 | `Infrastructures/PollyConfig.cs` | `ResiliencePipeline` v8: **Retry(3, backoff exponencial 2^n → 1s/2s/4s)** → **CircuitBreaker(ratio 100%, mínimo 3 fallos, ventana 30s, abierto 30s)** → **Timeout(10s por intento)**, con logs Serilog en `OnRetry`/`OnOpened`/`OnClosed`/`OnHalfOpened`. Los builders (`EmailRetryOptions`, `EmailCircuitBreakerOptions`, `BuildEmailPipeline`) son públicos para poder testear con `delay: TimeSpan.Zero` | ⬜ |
| 6.3 | Envolver email | `MailKitEmailService.SendEmailAsync`: el bloque SMTP (`Connect→Auth→Send→Disconnect`, ahora dentro del callback para crear un `SmtpClient` por intento) va bajo `await _emailPipeline.ExecuteAsync(...)`. El pipeline se inyecta desde DI (singleton en `EmailConfig.AddEmail`) con parámetro opcional para los tests existentes | ⬜ |
| 6.4 | Fallback | Agota reintentos o circuito abierto → `LogWarning`/`LogError` en el servicio y el error se relanza… pero el caller es `EmailBackgroundService` (try/catch) → **el request HTTP nunca falla por email** | ⬜ |
| 6.5 | Doc/comentario | Comentario XML en `PollyConfig` comparándolo con el reintento a mano de `PedidosService.cs:40` (`MaxRetries = 3` + bucle `for` + `Task.Delay`): aquí backoff, cortacircuitos y timeout declarativos y testeables | ⬜ |
| 6.6 | Test | 5 tests nuevos en `Unit/Infrastructures/PollyConfigTests.cs`: falla 2× y al 3º OK (3 intentos) · agota reintentos (1+3=4 y propaga) · CB abre a los 3 y la 4ª llamada no ejecuta el callback (`BrokenCircuitException`) · pipeline completo: el retry no insiste con el circuito abierto | ⬜ |
| 6.7 | *(opt.)* Endpoint demo `GET /api/demo/polly` | **No realizado**: la API no tiene carpeta/controllador demo y añadiría superficie HTTP nueva solo con fines didácticos; los 5 tests unitarios cubren el comportamiento | — |
| 6.8 | Verificar | build **0/0** · unit **1039/1039** (+5) · integración **161/0/32** · runner **55/55** · smoke: health OK, Swagger 200, ETag→304 · logs **0 excepciones / 0 ERR** | ⬜ |

---

## Fase 7 — Automation E2E en Node (todos los controladores) ⬜ PENDIENTE (replicar)

> Estilo UD02 `ejemplos/*/automation/test-runner.mjs` (Node nativo, sin npm install).  
> **Directorio:** `TiendaApi.Tests.E2E/Automation/`

| # | Tarea | Detalle |
|---|-------|---------|
| 7.1 | ⬜ Crear `TiendaApi.Tests.E2E/Automation/test-runner.mjs` | Runner completo: docker compose (postgres+mongodb) → `dotnet restore/build/run` → suite HTTP → limpieza |
| 7.2 | Cobertura de **todos** los controllers | Ver tabla siguiente |
| 7.3 | Rate limit awareness | Helper `st()`: falla claro si 429 (100/15s · auth 10/min · POST 20/min) |
| 7.4 | Credenciales seed | `admin/admin`, `userdaw/userdaw` |
| 7.5 | Ejecución | `node TiendaApi.Tests.E2E/Automation/test-runner.mjs` desde la raíz del repo |
| 7.6 | CI (opcional) | Job GitHub Actions con Docker services + Node + .NET SDK |

### Cobertura por controlador

| Controller | Métodos cubiertos |
|------------|-------------------|
| **Health** | `GET /health` |
| **Auth** | `POST signup` (201 / 400), `POST signin` admin y user (200 / 401) |
| **Categorías** | `GET` paged, `GET/{id}`, 404, `POST` 401/403/201, `PUT`, `DELETE`, DELETE 404 |
| **Productos** | `GET` paged + filtros, `GET/{id}`, `GET/categoria/{id}`, 404, `POST` 401/201/400, `PUT`, `PATCH`, `DELETE` |
| **Pedidos (usuario)** | `GET me`, `GET me/paged`, `POST me` 201, `POST` sin auth 401, `GET/PUT me/{id}` |
| **Pedidos (admin)** | `GET` 401/403/200, `GET paged`, `GET/{id}`, `PUT estado`, `DELETE` |
| **Users (admin)** | `GET` 401/403/200, `GET/{id}`, 404, `POST`, `PUT`, `DELETE` |
| **Users (perfil)** | `GET/PUT me/profile`, 401 sin token |
| **Storage** | `GET /storage/...` 404 |
| **GraphQL** | queries `productos`, `categorias`, `producto(id)`; mutation sin auth → error |

*WebSockets/SignalR fuera del runner HTTP puro (sin dependencias npm); se quedan en Bruno.*

### Notas de diseño del runner

- **No** usa `docker compose down -v` al final: solo `stop` de servicios BD para no romper el entorno de desarrollo.
- Fallback: si `dotnet run` no responde, intenta `docker compose up -d --build`.
- Compatible con Fase 1: espera `/health` primero; si aún no existe, acepta `/swagger` o `/api/productos`.

### 📋 Verificación Fase 7 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 8 — Migraciones EF Core (índices y esquema en BD existente) ⬜ PENDIENTE (replicar)

> **Problema:** hoy `EnsureCreated` **no** altera BDs ya creadas → los índices de la Fase 1B **no se aplican** en producción ni en volúmenes Docker persistentes.  
> **Objetivo:** introducir **EF Core Migrations** sin romper el flujo de desarrollo.

| # | Tarea | Detalle |
|---|-------|---------|
| 8.1 | Design-time | Asegurar `Microsoft.EntityFrameworkCore.Design` (ya en csproj) + `IDesignTimeDbContextFactory<TiendaDbContext>` si hace falta para `dotnet ef` |
| 8.2 | Migración inicial | `dotnet ef migrations add InitialCreate` → script del esquema actual (tablas + índices únicos existentes) |
| 8.3 | Migración de índices | Tras Fase 1B: `dotnet ef migrations add AddOptimizationIndexes` → `CREATE INDEX` para `CategoriaId`, `CreatedAt`, `IsDeleted`, `(CategoriaId, Precio)`, `Role` |
| 8.4 | Arranque por entorno | **Producción:** `context.Database.Migrate()` en lugar de `EnsureCreated()` (`DatabaseInitializationExtensions.cs:45`)<br>**Desarrollo:** mantener `EnsureDeleted + EnsureCreated` **o** `Migrate()` tras drop (decidir; drop+create ya funciona) |
| 8.5 | BD existente viva | `Migrate()` aplica solo lo pendiente → índices **sin** perder datos |
| 8.6 | Docker | Volumen `postgres-data` persistente: con migraciones, el siguiente arranque crea índices; sin ellas, haría falta `Reset-Database.ps1` |
| 8.7 | Scripts | Mantener `Reset-Database.ps1` para reset local completo |
| 8.8 | Verificar | En BD ya creada sin índices → arranque → `\di` en PG muestra los índices nuevos; E2E en verde |

**Orden recomendado:** Fase 1B (definir índices en modelo) → **Fase 8** (migración que los materializa) → Fase 7 Automation valida todo.

### 📋 Verificación Fase 8 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 9 — Integración Result→HTTP · **Opción C** (`ToHttpResult`) ⬜ PENDIENTE (replicar)

> **Fuente:** doc UD02 §7 *Excepciones y patrón Result* (`UD02/07-excepciones-patron-result.md`) + ejemplo `UD02/ejemplos/07-ProductosResult/Extensions/DomainErrorExtensions.cs`.  
> **Viabilidad:** ⬜ **ALTA** — todos los prerrequisitos ya existen en la API: `DomainError` tipado (`NotFoundError`, `ValidationError`, `BusinessRuleError`, `ConflictError`, `UnauthorizedError`, `ForbiddenError`, `InternalError`), fábricas de error por dominio (`ProductoError`, `CategoriaError`, `UsuarioError`, `AuthError`, `PedidoError`, `StorageError`), `Result<T, DomainError>` + `Match` en los controladores y CSharpFunctionalExtensions 3.7.0.  
> **Situación actual:** **31 `error switch` inline** repetidos en 5 controladores (Users 9 · Pedidos 8 · Productos 7 · Categorías 5 · Auth 2).  
> **Por qué Opción C y no B** (§7.8.3): con 5+ controladores, la extensión única es la recomendada por el doc.

| # | Tarea | Detalle |
|---|-------|---------|
| 9.1 | Nuevo `Extensions/DomainErrorExtensions.cs` | `public static IActionResult ToHttpResult(this DomainError error)` con switch tipado. **Mapeo = códigos actuales** para no romper E2E: `NotFoundError`→404 · `ValidationError`→400 (+ `ValidationErrors`) · `ConflictError`→409 · `BusinessRuleError`→400 (según XML docs del proyecto) · `UnauthorizedError`→401 · `ForbiddenError`→403 · `InternalError`/default→500 (mensaje `error.Message`, igual que hoy) |
| 9.2 | Sustituir los 31 switches | `onFailure: error => error switch {...}` → `onFailure: error => error.ToHttpResult()` en los 5 controladores; conservar `Match`/`IsSuccess` en flujos simples (ej. DELETE) |
| 9.3 | Auditar mapeos divergentes | Switches actuales que no siguen la matriz (p. ej. `BusinessRuleError` hoy cae a 500 en algunos endpoints → con `ToHttpResult` pasaría a 400): anotar como mejora, decisión explícita |
| 9.4 | Tests | Actualizar aserciones de controlador afectadas (tipos `StatusCodeResult`/objetos) |
| 9.5 | Verificar | Build 0/0 · unit · Bruno/Newman (400/401/403/404/409) — ideal **tras la Fase 7**, que cubre todos los códigos |

**Riesgo:** 🟡 bajo — solo capa de presentación; obligatorio preservar códigos y shape `{message, ...}` de cada respuesta.

### 📋 Verificación Fase 9 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 10 — Documentación didáctica ⬜ PENDIENTE (replicar)

> **Regla:** **NO** crear documentos nuevos. Insertar secciones explicativas en el `doc/NN-*.md` **oportuno** para cada tema, con el estilo del resto del documento (código real del proyecto) y **actualizando su Índice**. Cubre **todas** las fases del plan, estén completas (⬜) o previstas.

| # | Tema | Fase(s) | Documento → sección |
|---|------|---------|---------------------|
| 10.1 | Result → HTTP con `ToHttpResult()` (Opción C) | 9 ⬜ | `doc/11-patron-result.md` → 11.6 Integración Result + Controladores |
| 10.2 | Caché de salida: `OutputCache` + invalidación por tags | 4 ⬜ | `doc/10-redis-caching.md` → nueva sección (antes del resumen) |
| 10.3 | `ETag` + revalidación `304` (patrón real del proyecto) | 4 ⬜ | `doc/06-rest-best-practices.md` → 6.7 ETag para Cacheo |
| 10.4 | Paginación real en BD (`Skip/Limit`, no en memoria) | 3 ⬜ | `doc/06-rest-best-practices.md` → 6.3 Paginación |
| 10.5 | Migraciones EF Core: factory design-time, `InitialCreate`/`AddOptimizationIndexes`, baseline en BD existente, dev vs prod | 8 ⬜ | `doc/08-ef-core-postgresql.md` → 8.5 Migraciones |
| 10.6 | `AsNoTracking` en consultas de solo lectura · índices de optimización | 2 ⬜ · 1 ⬜ | `doc/27-optimizacion.md` → 27.5 EF Core · 27.3 Índices |
| 10.7 | Fire & forget endurecido (`Task.Run` + try/catch) | FF ⬜ | `doc/22-background-jobs.md` |
| 10.8 | Polly educativa (Retry + CircuitBreaker + Timeout en email) | 6 ⬜ | `doc/13-pedidos-transacciones.md` → 13.3 · `doc/21-email-services.md` |
| 10.9 | Automation E2E en Node (runner de todas las fases) | 7 ⬜ | `doc/24-testing.md` → 24.15 (tras 24.14) |
| 10.10 | Verificar | ⬜ | **9 documentos modificados, 0 creados** · TOC↔headings coherentes en los 9 · build 0/0 · **1039 unit** (1034 + 5 de Polly) |

**Detalle de inserciones:** 11.6 → subsección "Opción C aplicada en el proyecto: `ToHttpResult()` (Fase 9)" (31 call sites en 5 controladores) · 10 → nueva **10.10 "Caché HTTP con OutputCache y ETag"** (Resumen → 10.11) · 06 → 6.3 subsección "Paginación real (Fase 3)" + 6.7 subsección "Patrón real (Fase 4)" · 08 → 8.5 subsecciones factory/baseline/dev-vs-prod · 27 → 27.3 subsección "Índices reales (Fase 1)" + 27.5 subsección "AsNoTracking selectivo (Fase 2)" · 22 → nueva **22.12 "Fire & Forget Endurecido"** (Resumen → 22.13) · 13 → 13.3 subsección "Polly en este proyecto: dónde está y dónde NO" + 21 → nueva **21.10 "Resiliencia con Polly (Fase 6)"** (Resumen → 21.11) · 24 → nueva **24.15 "Automation E2E con Node"**.

---

## Fase 11 — Infra Docker saludable + unificación de imágenes ⬜ PENDIENTE (replicar)

> **Nota de orden:** ejecutada **antes de la Fase 6** (Polly) por petición explícita. Va al final de la numeración porque el plan 0-10 ya estaba cerrado; el orden de ejecución real no es secuencial (9, 8, 7 antes que 5). Aplicable también al destino **CQRS**.

### Tareas

| # | Tarea | Detalle | Estado |
|---|-------|---------|--------|
| 11.1 | Unificar imágenes Docker | `mongo:7` → `mongo:7.0` en `docker-compose.local.yml` y `docker-compose.prod.yml`; literales de test centralizados en la nueva constante `TiendaApi.Tests/Integration/TestContainers/TestContainerImages.cs` (10 ficheros, 18 llamadas). Etiquetas finales únicas: `mongo:7.0` + `postgres:17-alpine` (tag legacy `mongo:7` eliminada de Docker) | ⬜ |
| 11.2 | Compose local: salud y orden de arranque | `start_period: 30s` en healthchecks de postgres y mongo; `depends_on: condition: service_healthy` en adminer y mongo-express | ⬜ |
| 11.3 | Compose prod: YAML roto + salud | **Fix indentación:** 3 líneas del `environment` de `api` tenían 7 espacios en vez de 6 (`MongoDbSettings__DatabaseName`, `MongoDbSettings__PedidosCollection`, `Pedidos__RepositoryType`); `start_period: 30s` en healthchecks (postgres/mongo/redis) | ⬜ |
| 11.4 | Compose E2E Bruno-Cli al patrón oficial | Reescrito con la imagen oficial `usebruno/cli` (entrypoint `bru`, workdir `/bruno`, verificada en el registry); `command: run . --env-file environments/local.bru --delay 3200` + reporters nativos (json/junit/html → `/reports`); `extra_hosts: host.docker.internal:host-gateway`; `restart: on-failure:3`; colección montada `:ro` | ⬜ |
| 11.5 | Compose E2E Postman-Cli | Colección y environment montados en el `working_dir` (antes estaban en `/etc/newman/collections` y el `run` no las encontraba); `--env-var baseUrl` (el env `BASE_URL` no lo leía Newman); `--delay-request 3200`; `restart: on-failure:3` | ⬜ |
| 11.6 | Reconexión a nivel de driver (Mongo) | `&retryWrites=true&retryReads=true` añadidos a las 12 connection strings `mongodb://` de 6 ficheros (`appsettings.json`/`Development`/`Production`, `.env.development`, `.env.example`, `docker-compose.prod.yml`) | ⬜ |
| 11.7 | `.gitignore` | Carpetas `TiendaApi.Tests.E2E/**/reports/` (informes generados por los compose) | ⬜ |
| 11.8 | Verificación | `docker compose config` 4/4 OK · stack local `up -d` → postgres y mongo **healthy** · `docker images` sin duplicados · build 0/0 · unit 1034/1034 · integración 161/0/32 (con `TestContainerImages`) | ⬜ |

### Hallazgos y decisiones

1. **`docker images` acumulaba `mongo:7` y `mongo:7.0`** con el mismo ID: el compose usaba `mongo:7` y los tests `mongo:7.0`. Ahora un solo tag en todos lados.
2. **Defaults de Testcontainers 4.15.0** son `mongo:6.0` y `postgres:15.1` (irrelevantes: todos los builders pasaban imagen explícita, pero ahora salen de una única constante).
3. **`EnableRetryOnFailure` de EF Core: NO se activa, deliberadamente.** `PedidosService.cs:458` usa `BeginTransactionAsync` (transacción explícita) y con *retrying strategy* EF lanza `InvalidOperationException`. El patrón oficial (`CreateExecutionStrategy().ExecuteAsync(...)`) tocaría el flujo central de `POST /api/pedidos/me` = riesgo alto fuera de alcance. La reconexión a nivel de driver de Mongo (11.6) sí aplica.
4. **Bruno por CLI:** no se puede ejecutar por tandas (`bru.setVar` no sobrevive entre invocaciones) → corrida única con `--delay 3200` (rate limit `POST:*` = 20/min).
5. `docker-compose.prod.yml` requiere un `.env` local (está en `.gitignore`); se valida con un temporal copiado desde `.env.prod.example`.

---

## Fase 12 — Corrección y actualización del README ⬜ PENDIENTE (replicar)

> **Alcance:** solo `README.md` (+112/−84 líneas). **Sin cambios de código.** Cierra la deuda documentada de la fase 10 con el README raíz.

### Tareas

| # | Tarea | Detalle | Estado |
|---|-------|---------|--------|
| 12.1 | Errores de comandos y puertos | `dotnet run --project TiendaApi.Apis` → **`TiendaApi.Api`** (no existía); acceso dev `localhost:5000` → **`localhost:5031`** (launchSettings real; 5000 es prod vía `API_PORT`); añadido `GET /health` al bloque de inicio; `docker-compose` (v1) → **`docker compose`**; `cp .env.example` → `cp .env.prod.example .env` en el bloque prod | ⬜ |
| 12.2 | Versión de BD | Tecnologías: `PostgreSQL 15` → **`PostgreSQL 17`** (`postgres:17-alpine`) | ⬜ |
| 12.3 | Rutas E2E inexistentes | `TiendaApi.ApiTests/{Postman,Bruno}` (carpeta que **no existe**) → **`TiendaApi.Tests.E2E/{Postman-Cli,Bruno-Cli,Bruno-Local,Automation}`** | ⬜ |
| 12.4 | Comandos E2E reescritos | Newman/Bruno con rutas reales, `--delay-request 3200` / `--delay 3200` (rate limit `POST:*` 20/min), `bru run … --env-file <fichero.json>` (`.json` soportado según `--help` de la CLI), requisito "API en :5031" y nota de los informes en `reports/` (gitignored) | ⬜ |
| 12.5 | Estado actual de las fases | Nueva subsección **Automation (Node)** (`test-runner.mjs`, auto/externo); pirámide de tests con cifras (**1039 unit · 161 integración · 95 Newman · 108 Bruno · 55 runner**); +5 características (`/health`, OutputCache+ETag/304, Polly, paginación real, `ToHttpResult`); **Polly 8** en tecnologías; typo `Tescontainers` | ⬜ |
| 12.6 | Estructura del proyecto | Retirado `docker-compose.yml` de raíz (**no existe**); subárbol E2E real (`Automation/`, `Postman-Cli/`, `Bruno-Local/`, `Bruno-Cli/`); añadidos `FASES-MEJORAS.md`, `BITACORA.md`, `.env.development`, `.env.prod.example`; `Services/Usuarios` → **`Services/Users`** (+ `Auth/`, `Cache/`), `Extensions/`; tabla: `StorageController`, `PedidosService`, `MailKitEmailService`, fila **Extensions** (`ToHttpResult()`) | ⬜ |
| 12.7 | Coherencia interna | TOC ↔ headings verificados con el algoritmo de anclas de GitHub (script): **66/66** · endpoints de ejemplo `localhost:5000` → `5031` (imagen GraphQL y WS) | ⬜ |

### Hallazgos y decisiones

1. **Bruno Local debe excluir `12 - WEBSOCKETS`**: verificado en vivo con `@usebruno/cli` **4.2.0** (temp) — `bru run .` arrastra esa carpeta y falla porque la variable `basews` no está en el environment. Se documenta la **lista explícita de 10 carpetas** → **64 requests** (idéntico al conteo de la Fase 5). `6 - USUARIOS` está vacía (PASS con 0 requests).
2. **`--env-file` acepta `.json`**: confirmado en `bru run --help` ("Path to environment file (.bru or .json)"), así que el environment JSON de `Bruno-Local` es válido para la CLI.
3. **Los composes E2E no levantan la API**: apuntan a `host.docker.internal:5031` → el README ahora lo declara como requisito de la sección.
4. El automation sí levanta su propia API (modo auto), por eso es el comando más cómodo para clase.

### 📋 Verificación Fase 12 — pendiente (ejecutar en CQRS y sustituir los valores del origen)

---

## Fase 13 — CQRS real: queries de Productos en MongoDB desnormalizado ⬜ PENDIENTE (nueva)

> **Alcance:** funcionalidad nueva, propia de este proyecto (no existe en el origen).
> **Escrituras → PostgreSQL · Queries → MongoDB** (colección `productos_read`, documento
> desnormalizado con, como mínimo, el nombre de la categoría), sincronizado por **eventos**
> (Domain Events + MediatR — apuntes §30.6.4 y §30.10).
> **Categorías: 100% PostgreSQL** (commands, queries, GraphQL, caché) — no se replica nada;
> únicos puntos de contacto: al renombrar una categoría se actualiza el nombre embebido en sus
> productos de `productos_read`, y la validación de existencia de `GET /productos/categoria/{id}`
> sigue consultando PG.
> **Base documental:** apuntes UD02 `30-cqrs-mediator.md` (§30.4 documentos denormalizados,
> §30.6.4 Domain Events, §30.10 sync con MediatR) + ejemplo `ejemplos/23-ProductosCQRS`
> (`ProductoRead`, `SyncProductoHandler`, `ProductoReadRepository`).
> **Prerrequisito:** Fase 0 (baseline) + Fase 5 (verificación global) en verde antes de tocar código.

### Flujo

```
ESCRITURA  Controller/GraphQL → Command MediatR → IProductoRepository (PostgreSQL)
                                                     │ objeto ya en memoria
                                                     ▼ await Publish(notificación)
LECTURA    SyncHandler → ReplaceOne upsert → MongoDB "productos_read"
             { id, nombre, descripcion, precio, stock, imagen, isDeleted,
               categoria: { id, nombre }, createdAt, updatedAt, syncAt }

           Query REST / GraphQL / reporte → IProductoService → caché → IProductoReadRepository (Mongo)
ARRANQUE   ProductoReadSeeder → dev: drop+bulk | prod: upsert+podar (tras SqlSeeder, ambos entornos)
```

### Tareas

| # | Tarea | Detalle | Estado |
|---|-------|---------|--------|
| 13.1 | Modelo de lectura | `Models/Read/ProductoRead.cs` + `CategoriaRead { Id, Nombre }` (mínimo: nombre de categoría; el contrato GraphQL `[065]` pide `categoria { nombre }`) + mapper `ProductoRead → ProductoDto` (plano, `categoriaNombre`) | ⬜ |
| 13.2 | Repositorio lector nativo | `IProductoReadRepository` + `ProductoReadRepository` (driver nativo, estilo `PedidosNativeRepository`): paged con filtros (`nombre` → regex escapado **case-sensitive** = `LIKE` de PG, `categoria` por nombre, `precioMax`, `stockMin`, `isDeleted` con la semántica del query filter global), sort whitelist (`id/nombre/precio/stock/createdat` → y `categoria` → `categoria.nombre`), count + Skip/Limit, findById/findByCategoriaId excluyendo borrados, `GetRecentlyCreatedAsync`, `UpsertAsync` (`ReplaceOneAsync` con `IsUpsert`), `SoftDeleteAsync` (marca `IsDeleted`, no borra), `UpdateCategoriaNombreAsync` (`updateMany`) | ⬜ |
| 13.3 | Infra Mongo | `MongoDbSettings:ProductosCollection = "productos_read"` (appsettings ×3 + `.env.example`); **`IMongoClient`/`IDatabase` registrados siempre** (hoy solo si `Pedidos:RepositoryType=MongoDbNative`); `IProductoReadRepository` en `RepositoriesConfig` sin depender del switch de pedidos | ⬜ |
| 13.4 | Sync por eventos | `Features/Productos/Sync/ProductoReadSyncHandler` → `INotificationHandler` de `ProductoCreado/Actualizado/EliminadoNotification` (los 5 commands **ya publican** con `ProductoDto` que incluye `categoriaNombre` — cero cambios en commands); `try/catch + LogError` para que un fallo de Mongo no tumbe un comando ya commiteado (PG = fuente de verdad, repara el seeder). `UpdateCategoriaCommand` publica `CategoriaActualizadaNotification` → handler con `updateMany({ "categoria.id": id }, { $set: { "categoria.nombre": nuevo } })` | ⬜ |
| 13.5 | Seeder inicial | `Data/Seed/Mongo/ProductoReadSeeder` llamado en `InitializeDatabaseAsync` en **dev y prod**: dev = drop + bulk insert tras `SqlSeeder` (PG se recrea cada boot); prod = upsert + poda de huérfanos. Obligatorio: E2E exige semilla en Mongo (`totalCount >= 3`, `GET /api/productos/1`) | ⬜ |
| 13.6 | Queries REST → Mongo | `IProductoService` + `ProductoService` (fachada de lectura estilo ejemplo 23: Mongo + caché con **claves/TTL idénticas** a hoy); los 3 handlers (`GetAll`, `GetById`, `GetByCategoria`) delegan tras sus validaciones (existencia de categoría sigue en PG) | ⬜ |
| 13.7 | GraphQL → Mongo | `TiendaQuery.GetProductos/GetProducto/GetProductosPaged` → servicio; `ProductoType` pasa a `ObjectType<ProductoRead>` + nuevo `CategoriaReadType` (nombre GraphQL distinto para no chocar con `CategoriaType`); **mutaciones y suscripciones no se tocan** (ya usan MediatR y eventos propios) | ⬜ |
| 13.8 | Reporte background | `ProductoReportTask` (`GetRecentlyCreatedAsync`) → read repo: toda lectura de productos, sea de donde sea, sale de Mongo | ⬜ |
| 13.9 | Tests | Ajustar unit de los 3 query handlers (mock del read repo/servicio) + `RepositoriesConfigTests`; **nuevos**: `ProductoReadSyncHandlerTests` (estilo `SyncHandlerTests` del ejemplo) + integración Testcontainers (command → evento → Mongo → query). **No se tocan** las colecciones E2E (shapes intactos) | ⬜ |
| 13.10 | Verificación global | Build 0 errores 0 warnings · unit · integración · Automation 55 · Bruno 108 (64 requests, lista de 10 carpetas) · Newman 95 | ⬜ |
| 13.11 | Documentación | Esta sección → completar con cifras reales; fila en `BITACORA.md` con hashes al ejecutar | ⬜ |

### Hallazgos y decisiones (análisis de viabilidad — 25/09/2026)

1. **Viabilidad confirmada**: la estructura CQRS/MediatR ya existe (`Features/Productos/{Commands,Queries,Notifications}`), los 5 commands publican notificaciones con `ProductoDto` completo (incluye `categoriaId` + `categoriaNombre`), Mongo nativo ya está infraestructurado para pedidos y el DTO de lectura ya es plano con `categoriaNombre` → **E2E intacto** (shapes sin cambiar).
2. **Lecturas dentro de comandos siguen en PG**: `CreatePedidoCommand` lee el precio del producto dentro de la transacción de escritura (consistencia fuerte al cobrar); eso no es un "query". Tampoco las validaciones de existencia de categoría.
3. **No existía `IProductoService`** en Tienda (a diferencia del ejemplo 23): se crea como fachada única de lectura para que REST, GraphQL y reporte compartan el mismo camino `servicio → caché → Mongo`.
4. **Soft delete**: PG usa `IsDeleted` con query filter global; el documento guarda `IsDeleted` y la réplica excluye por defecto (los E2E no usan `?isDeleted`, pero es paridad total y gratis).
5. **Cobertura de eventos**: `DecrementStockAsync` no tiene ningún llamador → todas las escrituras pasan por los 5 commands notificados (advertencia: si pedidos decrementa stock en el futuro, añadir sync ahí).
6. **Mongo debe registrar siempre**: hoy `DatabaseConfig` solo registra `IMongoClient` si `Pedidos:RepositoryType=MongoDbNative`; el read model no puede depender de ese switch.
7. **GraphQL**: el contrato E2E `[065]` exige `categoria { nombre }` → el documento embebe `categoria { id, nombre }`; solo cambia el tipo CLR de `ProductoType` (introspección), no los campos de las queries.

### Fuera de alcance (Fase 13)

| Tema | Motivo |
|------|--------|
| Kafka / CDC (apuntes §30.6.5–§30.8) | Infraestructura externa; la fase usa el enfoque recomendado §30.6.4/§30.10 (Domain Events con MediatR) |
| Réplica de **Categorías** a MongoDB | No aporta (dimensión pequeña); solo se embebe su nombre en `productos_read` |
| Usuarios en Mongo | Fuera del alcance de esta fase |
| Arreglar desajuste preexistente de claves de caché (`productos:all` vs `productos:paged:*`) | Bug latente también en el origen — aparte, para no mezclar con el cambio de fuente de lectura |

### 📋 Verificación Fase 13 — pendiente (ejecutar y sustituir con cifras reales)

---

## Fuera de alcance (confirmado)

| Tema | Motivo |
|------|--------|
| `await Task.WhenAll` en email/efectos | Bloquearía la respuesta HTTP — **rechazado** |
| Proyecciones `Select`→DTO en SQL | Riesgo alto GraphQL/SignalR/E2E — aparte |
| Rate limiting **nativo** `AddRateLimiter` | Ya está AspNetCoreRateLimit activo; no duplicar |
| OpenTelemetry / MiniProfiler | No pedido en esta iteración |
| Tocar transacciones `Serializable` de pedidos | Riesgo alto (flujo central `POST /api/pedidos/me`) |
| Polly sobre BD/HttpClient real | Sin caso de uso real en la API |

---

## Orden de ejecución

```
0.1 → 10 → 2 → FF → 1 (AsNoTracking) → 4 (pedidos paged)
   → 6-OutputCache → 9 (ToHttpResult)
   → 8 (migraciones/índices)
   → 7 (Automation) → 5.x (verificación global) → 13 (CQRS real: queries de productos en Mongo)
   → 11 (Docker/imágenes)
   → 6-Polly
   → 10 (documentación didáctica de todas las fases)
   → 12 (README al día con todas las anteriores)
```

> **Nota:** Fase 8 antes que 7 para que el Automation valide una BD con índices reales.  
> **Fase 9** va tras OutputCache y antes de 7: la Automation valida los códigos HTTP de la refactorización.  
> **Fase 11** (Docker/imágenes) ejecutada antes que 6, aunque numerada al final.  
> Las “6” son distintas: **Fase 4 = OutputCache (#6 del análisis)**; **Fase 6 = Polly**.  
> **Fase 12** (README) es la última: recoge el estado real de todas las anteriores.  
> **Fase 13** (nueva, propia de CQRS) va **justo tras 5.x**: el gate verde (Bruno/Newman/Automation) debe existir antes de cambiar la fuente de lectura de productos.
> **Fase 14** (paridad) va **justo tras 13**: ambas APIs deben comportarse idénticamente antes de replicar las fases 1-12.

---

## Fase 14 — Contrato de Paridad ⬜ PENDIENTE (nueva)

> **Alcance:** asegurar que ambas APIs (`TiendaDawApi-NetCore` y `TiendaDawApi-Cqrs-MediatR-NetCore`)
> se comportan de forma idéntica para el cliente. **Prerequisito** para replicar fases 1-12.
> **Regla de oro:** el cliente no debe saber si usa un proyecto u otro.

### Tareas

| # | Tarea | Verificación |
|---|-------|--------------|
| 14.1 | **Copiar `DomainErrorExtensions.cs`** del origen → CQRS (`TiendaApi.Api/Extensions/`) | Fichero idéntico al origen; `grep -r "ToHttpResult" Controllers/` |
| 14.2 | **Reemplazar switches inline** en Auth/Categorias/Pedidos/Users controllers por `error.ToHttpResult()` | Todos los controllers usan la extensión centralizada |
| 14.3 | **Restaurar `Postman-Cli/TiendaApi.NetCore.postman_environment.json`** desde el origen (limpio, sin datos de test, con `secret`/`enabled`) | `fc /B` entre ambos ficheros = idénticos |
| 14.4 | **Añadir `**/results.json`** al `.gitignore` | `git status` no muestra results.json |
| 14.5 | **Crear `CONTRATO-PARIDAD.md`** con reglas de paridad obligatoria | Documento existe y se referencia en FASES-MEJORAS |
| 14.6 | **Crear tests de integración de handlers MediatR** (equivalentes a los `Services/` del origen) | Tests pasan contra PostgreSQL + MongoDB reales |

### Diferencias conocidas que se resuelven en esta fase

| Diferencia | Severidad | Fix |
|-----------|-----------|-----|
| ValidationError sin campo `errors` en Auth/Categorias/Pedidos/Users | **ALTA** | Copiar `DomainErrorExtensions.cs` + usar `ToHttpResult()` |
| Postman environment.json sobrescrito por Newman (sin `secret`, con datos de test) | **BAJA** | Restaurar desde origen |
| `results.json` no está en `.gitignore` | **BAJA** | Añadir patrón |
| 5 ficheros `Services/` de integración faltantes en CQRS | **MEDIA** | Crear equivalentes CQRS (handlers MediatR) |

### Diferencias NO resueltas (pendientes de replicar en fases 1-12)

| Diferencia | Fase que la resuelve |
|-----------|---------------------|
| Sin `/health` endpoint | Fase 1 |
| Sin HTTP Output Cache (ETag/304) | Fase 4 |
| Error handling más estrecho (500 en vez de status correcto) | Fase 9 (ToHttpResult) |
| Sin índices EF en modelo | Fase 1 (índices) |
| Sin `AsNoTracking` selectivo | Fase 2 |

### Contenido mínimo del `CONTRATO-PARIDAD.md`

```
# Contrato de Paridad — TiendaDawApi

## Regla de oro
El cliente no debe saber si usa el proyecto A o el B.

## Paridad obligatoria
- Datos: mismas semillas (SqlSeeder + MongoDbSeeder) — byte-identical
- API: mismos endpoints, DTOs, errores { message, errors }, códigos HTTP
- Infraestructura: mismas imágenes Docker (postgres:17-alpine, mongo:7.0, redis:7-alpine)
- Testing: mismas colecciones E2E (Postman + Bruno), mismo automation
- Versiones: mismas versiones Testcontainers (4.15.0), NuGet (excepto MediatR/Polly)

## Excepciones permitidas
- MediatR 12.5.0 (solo CQRS) — patrón CQRS
- Polly 8.8.0 (solo origen) — resiliencia
- Diferencias de fases no replicadas (se resuelven al replicar)
```

---

## Resumen de impacto

| Fase | Impacto | Dificultad | Riesgo |
|------|---------|------------|--------|
| 0 Baseline | 🟡 Fundación | 🟢 Muy baja | 🟢 |
| 10 Health | 🟡 Ops | 🟢 Muy baja | 🟢 |
| 2 Índices (modelo) | 🟢 Alto | 🟢 Muy baja | 🟢 |
| FF Task.Run | 🟡 Calidad | 🟢 Baja | 🟢 |
| 1 AsNoTracking | 🟢 Alto | 🟢 Baja | 🟡 |
| 4 Pedidos paged | 🟢 Alto | 🟡 Media | 🟡 |
| 4 OutputCache | 🟡 Medio | 🟢 Baja | 🟡 |
| 8 Migraciones | 🟢 Alto (prod) | 🟡 Media | 🟡 |
| 7 Automation | 🟢 Muy alto (QA) | 🟡 Media | 🟢 |
| 9 ToHttpResult | 🟢 Mantenibilidad | 🟢 Baja | 🟡 |
| 6 Polly | 🟡 Educativo | 🟢 Baja | 🟢 |
| 5 Verificación global | 🟡 QA (E2E en vivo) | 🟡 Media | 🟢 |
| 10 Documentación didáctica | 🟡 Doc | 🟡 Media | 🟢 |
| 11 Docker/imágenes | 🟡 Ops | 🟡 Media | 🟡 |
| 12 README | 🟡 Doc/DAQ | 🟢 Muy baja | 🟢 |
| 13 Queries Productos→Mongo | 🔴 Funcionalidad nueva (CQRS real) | 🟡 Media | 🟡 |
| 14 Contrato Paridad | 🔴 Crítico (ambas APIs idénticas) | 🟢 Baja | 🟢 |

---

## Cómo ejecutar la Automation (Fase 7)

```bash
# Desde la raíz del repo (requiere Docker + .NET 10 + Node 18+)
node TiendaApi.Tests.E2E/Automation/test-runner.mjs

# Contra una API ya levantada
BASE_URL=http://localhost:5031 node TiendaApi.Tests.E2E/Automation/test-runner.mjs
```

Salir con código `0` si todo OK, `1` si algún test falla (listo para CI).
