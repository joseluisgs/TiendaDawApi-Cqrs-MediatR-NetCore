using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Tests.Unit.Helpers;

/// <summary>
/// Tests directos del header Link RFC 5988 de PaginationLinksHelper.
///
/// Se prueban aqui (helper puro, sin BD ni mocks) las reglas de los cuatro
/// enlaces first/prev/next/last: en la primera pagina NO hay first/prev, en la
/// ultima NO hay next/last, y cada enlace reconstruye su propia query con
/// page+size (y el orden si se paso) sin heredar la query original.
/// </summary>
public class PaginationLinksHelperTests
{
    /// <summary>
    /// Request de ejemplo: mismo host/puerto/path para todos los tests, para
    /// que las comparaciones de URL sean estables.
    /// </summary>
    private static HttpRequest CrearRequest(string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("api.local", 8080);
        context.Request.Path = "/api/productos";
        context.Request.QueryString = new QueryString(query);
        return context.Request;
    }

    private static PagedResult<string> CrearResultado(int page, int pageSize, int totalCount)
        => new() { Items = Array.Empty<string>(), Page = page, PageSize = pageSize, TotalCount = totalCount };

    [Test]
    public void CreateLinkHeader_ConUnaPagina_DevuelveVacio()
    {
        // Una sola pagina no necesita navegacion: header ausente (no ruido inutil)
        var resultado = CrearResultado(page: 1, pageSize: 10, totalCount: 10);

        var header = PaginationLinksHelper.CreateLinkHeader(resultado, CrearRequest());

        header.Should().BeEmpty();
    }

    [Test]
    public void CreateLinkHeader_PrimeraPagina_DejaSoloNextYLast()
    {
        // Page 1 de 5: no hay atras (first/prev) pero si adelante
        var resultado = CrearResultado(page: 1, pageSize: 10, totalCount: 50);

        var header = PaginationLinksHelper.CreateLinkHeader(resultado, CrearRequest());

        header.Should().Contain("rel=\"next\"");
        header.Should().Contain("page=2");            // siguiente
        header.Should().Contain("rel=\"last\"");
        header.Should().Contain("page=5");            // ultima
        header.Should().NotContain("rel=\"first\"");
        header.Should().NotContain("rel=\"prev\"");
    }

    [Test]
    public void CreateLinkHeader_UltimaPagina_DejaSoloFirstYPrev()
    {
        // Page 5 de 5: no hay adelante pero si atras
        var resultado = CrearResultado(page: 5, pageSize: 10, totalCount: 50);

        var header = PaginationLinksHelper.CreateLinkHeader(resultado, CrearRequest());

        header.Should().Contain("rel=\"first\"");
        header.Should().Contain("rel=\"prev\"");
        header.Should().Contain("page=4");            // anterior
        header.Should().NotContain("rel=\"next\"");
        header.Should().NotContain("rel=\"last\"");
    }

    [Test]
    public void CreateLinkHeader_PaginaIntermedia_DejaLosCuatroEnlaces()
    {
        // Page 3 de 5: navegacion completa en ambos sentidos
        var resultado = CrearResultado(page: 3, pageSize: 10, totalCount: 50);

        var header = PaginationLinksHelper.CreateLinkHeader(resultado, CrearRequest());

        header.Should().Contain("rel=\"first\"");
        header.Should().Contain("rel=\"prev\"");
        header.Should().Contain("rel=\"next\"");
        header.Should().Contain("rel=\"last\"");
        // Cada enlace apunta a su pagina concreta
        header.Should().MatchRegex(@"page=1[^>]*>; rel=""first""");
        header.Should().MatchRegex(@"page=2[^>]*>; rel=""prev""");
        header.Should().MatchRegex(@"page=4[^>]*>; rel=""next""");
        header.Should().MatchRegex(@"page=5[^>]*>; rel=""last""");
    }

    [Test]
    public void CreateLinkHeader_ConOrdenamiento_SeConservaEnCadaEnlace()
    {
        // El orden forma parte del estado de la lista: sin el, "siguiente"
        // volveria a la misma pagina con otro orden
        var resultado = CrearResultado(page: 2, pageSize: 20, totalCount: 100);

        var header = PaginationLinksHelper.CreateLinkHeader(
            resultado, CrearRequest(), sortBy: "nombre", direction: "asc");

        header.Should().Contain("sortBy=nombre");
        header.Should().Contain("direction=asc");
        header.Should().Contain("page=3");            // el resto del enlace sigue funcionando
    }

    [Test]
    public void CreateLinkHeader_QueryOriginal_NoSeHeredaEnLosEnlaces()
    {
        // Los enlaces son autocontenidos (page+size[+orden]): arrastrar filtros
        // de la query original duplicaria el parametro si el cliente la re-enge
        var resultado = CrearResultado(page: 2, pageSize: 10, totalCount: 50);

        var header = PaginationLinksHelper.CreateLinkHeader(
            resultado, CrearRequest(query: "?categoria=7"));

        header.Should().Contain("rel=\"next\"");
        header.Should().NotContain("categoria=7");
    }

    [Test]
    public void CreateLinkHeader_PaginaFueraDeRango_NoLanza_YDevuelveVacio()
    {
        // TotalCount=0 -> TotalPages=0 -> sin navegacion posible; el helper
        // responde vacio en vez de lanzar (su contrato es "header o nada")
        var resultado = CrearResultado(page: 1, pageSize: 10, totalCount: 0);

        var header = PaginationLinksHelper.CreateLinkHeader(resultado, CrearRequest());

        header.Should().BeEmpty();
    }
}
