using HotChocolate.Types;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para el proveedor embebido en el producto (read model de MongoDB).
///
/// Nombre GraphQL «ProveedorRead» (y no «Proveedor») para no chocar con
/// <see cref="ProveedorType"/>: el nombre solo afecta a la introspección, las
/// queries del cliente (<c>proveedor { nombre }</c>) siguen funcionando igual.
/// </summary>
public class ProveedorReadType : ObjectType<ProveedorRead>
{
    /// <summary>
    /// Configura el nombre, la descripción y los campos del tipo GraphQL.
    /// </summary>
    protected override void Configure(IObjectTypeDescriptor<ProveedorRead> descriptor)
    {
        descriptor.Name("ProveedorRead");
        descriptor.Description("Proveedor embebido en el producto (read model)");

        descriptor.Field(p => p.Codigo).Type<NonNullType<StringType>>().Description("El código del proveedor");
        descriptor.Field(p => p.Nombre).Type<NonNullType<StringType>>().Description("El nombre del proveedor");
    }
}
