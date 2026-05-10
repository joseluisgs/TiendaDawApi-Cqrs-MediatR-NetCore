$mappings = @(
    @{ Path="doc/01-configuracion-proyectos-dotnet.md"; Old=1; New=1; Title="Configuración de Proyectos .NET" },
    @{ Path="doc/02-arquitectura-pipeline-http.md"; Old=2; New=2; Title="Arquitectura Pipeline HTTP" },
    @{ Path="doc/03-inyeccion-dependencias.md"; Old=3; New=3; Title="Inyección Dependencias" },
    @{ Path="doc/04-controladores-rest.md"; Old=4; New=4; Title="Controladores REST" },
    @{ Path="doc/05-validacion-cascada.md"; Old=5; New=5; Title="Validación en Cascada" },
    @{ Path="doc/06-rest-best-practices.md"; Old=18; New=6; Title="REST Best Practices" },
    @{ Path="doc/07-repository-pattern.md"; Old=7; New=7; Title="Repository Pattern" },
    @{ Path="doc/08-ef-core-postgresql.md"; Old=9; New=8; Title="EF Core PostgreSQL" },
    @{ Path="doc/09-mongodb.md"; Old=10; New=9; Title="MongoDB" },
    @{ Path="doc/10-redis-caching.md"; Old=11; New=10; Title="Redis Caching" },
    @{ Path="doc/11-patron-result.md"; Old=6; New=11; Title="Patrón Result" },
    @{ Path="doc/12-cqrs-commands-queries.md"; Old=8; New=12; Title="CQRS Commands Queries" },
    @{ Path="doc/13-servicios-negocio.md"; Old=8; New=13; Title="Servicios Negocio" },
    @{ Path="doc/14-mediatr-cqrs-eventos.md"; Old=31; New=14; Title="MediatR Eventos" },
    @{ Path="doc/15-pedidos-transacciones.md"; Old=15; New=15; Title="Pedidos y Transacciones" },
    @{ Path="doc/16-mapeadores.md"; Old=22; New=16; Title="Mapeadores" },
    @{ Path="doc/17-jwt-authentication.md"; Old=12; New=17; Title="JWT Authentication" },
    @{ Path="doc/18-autorizacion-roles.md"; Old=13; New=18; Title="Autorización Roles" },
    @{ Path="doc/19-seguridad-http.md"; Old=27; New=19; Title="Seguridad HTTP" },
    @{ Path="doc/20-websockets.md"; Old=14; New=20; Title="WebSockets" },
    @{ Path="doc/21-graphql.md"; Old=20; New=21; Title="GraphQL" },
    @{ Path="doc/22-file-storage.md"; Old=16; New=22; Title="File Storage" },
    @{ Path="doc/23-email-services.md"; Old=17; New=23; Title="Email Services" },
    @{ Path="doc/24-background-jobs.md"; Old=25; New=24; Title="Background Jobs" },
    @{ Path="doc/25-documentacion-api.md"; Old=19; New=25; Title="Documentación API" },
    @{ Path="doc/26-testing.md"; Old=21; New=26; Title="Testing" },
    @{ Path="doc/27-docker-ci-cd.md"; Old=23; New=27; Title="Docker" },
    @{ Path="doc/28-logging.md"; Old=24; New=28; Title="Logging" },
    @{ Path="doc/29-optimizacion.md"; Old=26; New=29; Title="Optimización" },
    @{ Path="doc/30-ci-cd.md"; Old=28; New=30; Title="CI/CD" },
    @{ Path="doc/31-clean-architecture.md"; Old=29; New=31; Title="Clean Architecture" },
    @{ Path="doc/32-organizacion-program.md"; Old=30; New=32; Title="Organización Program.cs" }
)

foreach ($m in $mappings) {
    Write-Host "Processing $($m.Path)..."
    $content = Get-Content $m.Path -Raw
    $oldNum = $m.Old
    $newNum = $m.New
    $title = $m.Title
    
    # Update H1
    $content = $content -replace "(?m)^#\s+\d+\.\s+.*", "# $newNum. $title"
    
    # Update H2, H3, H4 with number.
    $content = $content -replace "(?m)^##\s+$oldNum\.", "## $newNum."
    $content = $content -replace "(?m)^###\s+$oldNum\.", "### $newNum."
    $content = $content -replace "(?m)^####\s+$oldNum\.", "#### $newNum."
    
    # Update Index/TOC entries
    $content = $content -replace "\[$oldNum\.", "[$newNum."
    
    # Anchors: replace (#18 with (#6
    # We use [regex]::Escape to be safe, but here it's simple
    $content = $content -replace "\(#$oldNum", "(#$newNum"
    
    Set-Content $m.Path $content -NoNewline
}
