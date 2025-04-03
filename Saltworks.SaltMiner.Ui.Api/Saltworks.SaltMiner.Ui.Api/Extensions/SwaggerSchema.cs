using Microsoft.OpenApi.Models;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Saltworks.SaltMiner.Ui.Api.Extensions
{
    public static class SwaggerSchema
    {
        public class AdditionalSchemasDocumentFilter : IDocumentFilter
        {
            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                context.SchemaGenerator.GenerateSchema(typeof(TemplateIssueImport), context.SchemaRepository);
                context.SchemaGenerator.GenerateSchema(typeof(IssueImportSummary), context.SchemaRepository);
                context.SchemaGenerator.GenerateSchema(typeof(AssetImport), context.SchemaRepository);
                context.SchemaGenerator.GenerateSchema(typeof(IssueImport), context.SchemaRepository);
            }
        }
    }
}
