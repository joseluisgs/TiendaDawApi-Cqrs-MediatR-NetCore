# 🐳 Docker y CI/CD en TiendaDawApi (CQRS)

> Contenedores para desarrollo/producción + el pipeline de GitHub Actions real
> del repositorio (`.github/workflows/ci.yml`). Todo tomado de los ficheros
> reales; el Dockerfile y el workflow son **idénticos** a los del proyecto
> origen (TiendaDawApi-NetCore).

---

## 1. El Dockerfile (multi-stage)

Fichero: `TiendaApi.Api/Dockerfile`.

| Etapa | Imagen | Qué hace |
|---|---|---|
| **build** | `mcr.microsoft.com/dotnet/sdk:10.0` | `.slnx` + `Directory.Packages.props` → `dotnet restore` (capa cacheada) → `dotnet publish Release` |
| **runtime** | `mcr.microsoft.com/dotnet/aspnet:10.0` | Solo el publish, sin SDK ni fuentes |

```dockerfile
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "TiendaApi.Api.dll"]
```

🎓 Dos etapas = imagen final pequeña y sin herramientas de compilación; la API
escucha en el **8080 interno** y el puerto del host lo pone el compose.

## 2. Composes

| Fichero | Servicios | Uso |
|---|---|---|
| `docker-compose.local.yml` | postgres `5432`, mongodb `27017`, adminer `8080`, mongo-express `8081`, cassandra `9042` (healthchecks en las BD) | **Desarrollo**: solo infra; la API corre fuera con `dotnet run` (http://localhost:5031) |
| `docker-compose.prod.yml` | postgres, mongodb, redis `6379`, cassandra y **api** (`${API_PORT:-5000}:8080`) | **Producción**: todo contenedorizado con healthchecks y volúmenes nombrados |

```bash
docker compose -f docker-compose.local.yml up -d      # infra de desarrollo
docker compose -f docker-compose.prod.yml up -d --build   # producción (lee .env)
docker compose -f docker-compose.prod.yml down -v     # ⚠ borra volúmenes (datos)
```

También hay composes de **E2E en contenedor**:
`TiendaApi.Tests.E2E/Bruno-Cli/docker-compose.yml` y
`TiendaApi.Tests.E2E/Postman-Cli/docker-compose.yml` (colecciones Bruno/Newman
sin instalar nada en la máquina).

## 3. Pipeline de CI — `.github/workflows/ci.yml`

**Disparadores**: `push`, `pull_request` y `workflow_dispatch` (manual).

| Job | `needs` | Qué hace |
|---|---|---|
| **build** | — | Checkout → setup .NET → `dotnet restore` → `dotnet build --configuration Release --no-restore` → sube artefactos del build |
| **test** | build | Descarga artefactos → `dotnet test --filter "FullyQualifiedName~Unit"` (unit, paralelo, con `XPlat Code Coverage`) → **ReportGenerator** crea el informe HTML de cobertura desde `coverage.cobertura.xml` → lo sube como artefacto |
| **test-integration** | build | `dotnet test --filter "FullyQualifiedName~Integration"` **secuencial** (los tests con contenedores no se paralelizan) |
| **validate-docs** | — | Instala **DocFX** → `docfx docfx.json` → sube la documentación HTML generada |
| **summary** | build, test, validate-docs | Comentario resumen del pipeline |

🎓 **Por qué separar unit e integration**: los unit van en paralelo (rápidos y
aislados); los integration usan Testcontainers (PostgreSQL/MongoDB/Cassandra
reales) y se ejecutan **en secuencia** para no pelearse por puertos y recursos
de Docker.

## 4. Comandos rápidos

```bash
# Ver los flujos
gh run list                      # o la pestaña Actions de GitHub

# Local, sin CI: lo mismo que hace el pipeline
dotnet build --configuration Release
dotnet test --filter "FullyQualifiedName~Unit"
dotnet test --filter "FullyQualifiedName~Integration"   # requiere Docker
```
