using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Viw.WebApi.OpenApi;

/// <summary>Reutiliza a documentação XML das propriedades dos filtros nos parâmetros de consulta agrupados.</summary>
internal sealed class QueryParameterDocumentationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        if (operation.Parameters is null) return;
        var schemas = new Dictionary<Type, IOpenApiSchema>();
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In != ParameterLocation.Query || !string.IsNullOrEmpty(parameter.Description)) continue;
            var description = context.Description.ParameterDescriptions.SingleOrDefault(item => item.Name == parameter.Name);
            var metadata = description?.ModelMetadata;
            if (description?.Source != BindingSource.Query || metadata?.ContainerType is not { } container || metadata.PropertyName is not { } name) continue;
            if (!schemas.TryGetValue(container, out var schema))
            {
                schema = await context.GetOrCreateSchemaAsync(container, cancellationToken: ct);
                schemas.Add(container, schema);
            }
            parameter.Description = schema.Properties?.FirstOrDefault(property =>
                string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase)).Value?.Description;
        }
    }
}
