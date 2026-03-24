/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

﻿using Microsoft.OpenApi;
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
