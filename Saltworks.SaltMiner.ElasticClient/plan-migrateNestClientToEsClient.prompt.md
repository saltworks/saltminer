## Prompt:
Goal is to complete implementation of EsClient* classes to replace NestClient* classes, so we can remove the Nest nuget package and use the newer Elastic.Clients.Elasticsearch nuget package to replace it.

A previous attempt was made to do this but was abandoned because the new nuget library was too incomplete to use.

Requirements would include (in no order):
1. Review all existing members in the main EsClient class, looking for differences with their NestClient counterparts that are in addition to the conversion needed.  For any that seem to have changed, flag for copy/convert, replacing previous (but can use previous work to help speed the conversion).
2. Identify the members in the main EsClient class that are missing (IElasticClient), then copy over the matching member from NestClient, correcting implementation where needed to work with the new package.
3. Copy existing EsClient* classes to EsOldClient* so that human reviews are easier between the final EsClient* classes and the original conversions.
4. Eliminate PitPagingInfo and UIPagingInfo classes in EsClient* classes, replacing with PagingInfo.
5. Refactor all tests in the IntegrationTests project to make use of the new EsClient.
6. Refactor tests to create their own temp indices and remove them once the test is complete, no dependencies on existing indices allowed.


## Plan: Migrate NestClient to EsClient for Elastic.Clients.Elasticsearch Package

This migration replaces the deprecated NEST package with Elastic.Clients.Elasticsearch by completing the EsClient implementation and updating all tests to use it.

### Steps

1. **Backup Original EsClient Work**: Copy [EsClient/EsClient.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClient.cs), [EsClient/EsClientResponse.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientResponse.cs), [EsClient/EsClientResult.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientResult.cs), [EsClient/EsClientException.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientException.cs), [EsClient/EsClientFactory.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientFactory.cs), and [EsClient/EsClientRequestAggregation.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientRequestAggregation.cs) to `EsOldClient*` equivalents for comparison during review.

2. **Complete Missing Interface Methods**: Implement all `IElasticClient` interface members currently throwing `NotImplementedException` in [EsClient/EsClient.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClient.cs) (e.g., `BulkPartialUpdate`, `GetCompositeAggregate`, `RegisterBackupRepository`, `SearchWithCompositeAgg`, `GetAggregate`). Port implementations from [NestClient/NestClient.cs](Saltworks.SaltMiner.ElasticClient/NestClient/NestClient.cs), converting NEST API calls to Elastic.Clients.Elasticsearch equivalents.

3. **Unify Paging Classes**: Replace `PitPagingInfo` and `UIPagingInfo` with single `PagingInfo` class throughout [EsClient/EsClient.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClient.cs), [EsClient/EsClientResponse.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientResponse.cs), eliminating dual paging systems and aligning with [NestClient/NestClientResponse.cs](Saltworks.SaltMiner.ElasticClient/NestClient/NestClientResponse.cs) approach.

4. **Review and Reconcile Method Implementations**: Compare each implemented method in `EsClient` against `NestClient` counterparts, identifying logic differences beyond API conversion (search methods around line 1200+, CRUD operations, index management). Update `EsClient` methods where business logic has evolved in `NestClient`.

5. **Migrate Integration Tests**: Update [IntegrationTests/Helpers.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/Helpers.cs) to use `EsClient` instead of `NestClient`, modify all test files ([AggregationTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/AggregationTests.cs), [ConnectionTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/ConnectionTests.cs), [CRUDTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/CRUDTests.cs), [ExceptionTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/ExceptionTests.cs), [IndexTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/IndexTests.cs), [RoleTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/RoleTests.cs), [SnapshotTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/SnapshotTests.cs), [UtilityTests.cs](Saltworks.SaltMiner.ElasticClient.IntegrationTests/UtilityTests.cs)) to create/cleanup temporary test indices, eliminating hardcoded index dependencies.

6. **Update Factory and Extensions**: Modify [EsClient/EsClientFactory.cs](Saltworks.SaltMiner.ElasticClient/EsClient/EsClientFactory.cs) and [Extensions/ConfigureClientExtensions.cs](Saltworks.SaltMiner.ElasticClient/Extensions/ConfigureClientExtensions.cs) to ensure proper `EsClient` instantiation and configuration, removing any `NestClient` references.

### Further Considerations

1. **API Compatibility**: Elastic.Clients.Elasticsearch has significant breaking changes from NEST - which methods require the most adaptation? Should we document API migration patterns for future reference?

2. **Performance Testing**: After migration, should we add performance benchmarks to validate the new client performs comparably to NEST?

3. **Deprecation Strategy**: Once `EsClient` is complete and tested, do we immediately remove `NestClient*` classes or maintain them temporarily with deprecation warnings for downstream consumers?

4. **PagingInfo Migration**: Should the unified `PagingInfo` class retain both PIT (Point-in-Time) and UI paging capabilities, or split into separate subclasses? This affects [tests using PitPagingInfo](Saltworks.SaltMiner.ElasticClient.IntegrationTests/CRUDTests.cs).