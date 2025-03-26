using System;
using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepository
    {
        /// <summary>
        /// Adds or updates the passed entity in the datasource
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="entity">Entity to add/update</param>
        /// <returns>The updated/inserted entity, including any updates made during the operation</returns>
        T AddUpdate<T>(T entity) where T : class;
        /// <summary>
        /// Adds or updates the passed list of entities in the datasource
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="entities">Entities to add/update</param>
        /// <returns>The updated/inserted entities, including any updates made during the operation</returns>
        List<T> AddUpdateMany<T>(List<T> entities) where T : class;
        /// <summary>
        /// Deletes an entity of type T from the datasource by its id
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="id">The identifier for the entity to be removed</param>
        /// <returns>True if successful, False if not</returns>
        bool Delete<T>(string id) where T : class;
        /// <summary>
        /// Deletes a list of entity ids from the datasource by its id
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="ids">The identifier for the entity to be removed</param>
        /// <returns>True if successful, False if not</returns>
        bool DeleteMany<T>(List<string> ids) where T : class;
        /// <summary>
        /// Returns an entity of type T from the datasource by its id
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="id">The identifier for the entity to be retrieved</param>
        /// <returns>The requested entity, or a default value if not found</returns>
        T Get<T>(string id) where T : class;
        /// <summary>
        /// Returns a list of entities of type T that match the passed filter criteria
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="filter">The criteria for the search</param>
        /// <param name="pager">Pagination information (implementation specific)</param>
        /// <returns>The list of results</returns>
        IDataRepositoryQueryResult<T> Search<T>(IDataFilter filter) where T : class;
        /// <summary>
        /// Configures entity type to datastore index/table relationship
        /// </summary>
        /// <param name="mappings">The mappings to configure for this IDataRepository</param>
        void Configure(Dictionary<Type, string> mappings);
    }
}
