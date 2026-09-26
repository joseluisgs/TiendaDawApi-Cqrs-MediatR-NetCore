# Contrato de Paridad — TiendaDawApi

> **Proyectos:** `TiendaDawApi-NetCore` (origen, API clásica por servicios) y
> `TiendaDawApi-Cqrs-MediatR-NetCore` (réplica CQRS + MediatR).
> Este documento define lo que **debe** ser idéntico en ambos y las **excepciones permitidas**.
> Detalle de la fase en `FASES-MEJORAS.md` → "Fase 14".

## Regla de oro

El cliente no debe saber si usa el proyecto A o el B.

## Paridad obligatoria

- **Datos:** mismas semillas (`SqlSeeder` + `MongoDbSeeder`) — byte-identical.
- **API:** mismos endpoints, mismos DTOs/shapes, mismos errores `{ message, errors }`, mismos códigos HTTP.
- **Infraestructura:** mismas imágenes Docker (`postgres:17-alpine`, `mongo:7.0`, `redis:7-alpine`).
- **Testing:** mismas colecciones E2E (Postman + Bruno) y mismo runner de Automation (55 checks).
- **Versiones:** mismas versiones de Testcontainers (4.15.0) y de NuGet, excepto las excepciones.

## Excepciones permitidas

| Excepción | Motivo |
|-----------|--------|
| MediatR 12.5.0 (solo CQRS) | Patrón CQRS/MediatR del proyecto destino |
| Polly 8.8.0 (solo origen) | Resiliencia educativa (Fase 6) — se replica después |
| Diferencias de fases aún no replicadas | Se resuelven al replicar cada fase (ver tabla inferior) |
| GraphQL: `Categoria.productos` **no expuesto** en el CQRS | Navegación EF de PostgreSQL; exponerla rompería CQRS (lecturas por queries) y chocaba con `ObjectType<ProductoRead>` ("Producto"). En el origen sí se expone. Ver `FASES-MEJORAS.md` → Fase 13, hallazgo 8 |
| Fases nuevas del CQRS (Fase 13: read model Mongo) | Funcionalidad interna: el contrato HTTP/GraphQL visible no cambia |

## Diferencias pendientes (se resuelven al replicar)

| Diferencia | Fase que la resuelve |
|-----------|---------------------|
| Sin endpoint `/health` | Fase 1 |
| Sin índices EF en el modelo | Fase 1 |
| Sin `AsNoTracking` selectivo | Fase 2 |
| Sin paginación real de pedidos | Fase 3 |
| Sin HTTP Output Cache (ETag/304) | Fase 4 |
| Migraciones EF Core con baseline | Fase 8 |
| Automation E2E en Node | Fase 7 |

> Estado actual (Fase 14): `DomainErrorExtensions.ToHttpResult()` ya centraliza el mapeo
> `DomainError → HTTP` en los 5 controllers (31 sitios), con el shape `{ message, errors }`.

## Verificación de paridad

```bash
# 1. Ambas APIs en verde con las MISMAS suites
node TiendaApi.Tests.E2E/Automation/test-runner.mjs   # 55 checks
# Newman: 4 tandas (61 s entre ellas) sobre Postman-Cli
# Bruno-Local: 10 carpetas con --delay 3200

# 2. Diferencias estructurales entre repos
#    - DomainErrorExtensions.cs byte-identico (fc /B)
#    - postman_environment byte-identico (fc /B)
#    - 0 "error switch" inline en Controllers/ (solo ToHttpResult)
```

Cifras esperadas por suite (Fase 5/Fase 13): build 0/0 · unit+integración 965/965 ·
Automation 54/55 (solo `/health`) · Newman 95 assertions (2 fallos esperados de `/health`) ·
Bruno 125/127 (los 2 de `/health`).
