using HotChocolate.Types;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la categoría embebida en el producto (read model de MongoDB).
///
/// Nombre GraphQL «CategoriaRead» (y no «Categoria») para no chocar con
/// <see cref="CategoriaType"/>: el nombre solo afecta a la introspección, las
/// queries del cliente (<c>categoria { nombre }</c>) siguen funcionando igual.
/// </summary>
public class CategoriaReadType : ObjectType<CategoriaRead>
{
    protected override void Configure(IObjectTypeDescriptor<CategoriaRead> descriptor)
    {
        descriptor.Name("CategoriaRead");
        descriptor.Description("Categoría embebida en el producto (read model)");

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>().Description("El ID de la categoría");
        descriptor.Field(c => c.Nombre).Type<NonNullType<StringType>>().Description("El nombre de la categoría");
    }
}
