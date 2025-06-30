/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.OpenApi.Models;
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
