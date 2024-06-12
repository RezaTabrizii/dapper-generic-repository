using DapperGenericRepository.Models.Parameters;
using DapperGenericRepository.Models.Results;

namespace DapperGenericRepository.Contracts
{
    public interface IDapperBaseRepository<TEntity>
    {
        #region Synchronous Methods (Query)

        void ExecuteQuery(string query, object param);
        void ExecuteManyQueries(List<string> queries, IEnumerable<object> parameters);
        void Insert(TEntity entity, bool containsId = false);
        void InsertMany(IEnumerable<TEntity> entities, bool containsId = false);
        void Replace(TEntity entity);
        void Replace(TEntity entity, ConditionParams customCondition);
        void ReplaceMany(IEnumerable<TEntity> entities);
        void ReplaceMany(IEnumerable<TEntity> entities, List<ConditionParams> customConditions);
        void Update(object param);
        void Update(object param, ConditionParams customCondition);
        void UpdateMany(IEnumerable<object> param);
        void UpdateMany(IEnumerable<object> param, List<ConditionParams> customConditions);
        void Delete(object id);
        void Delete(ConditionParams customCondition);
        void DeleteMany(IEnumerable<object> ids);
        TEntity FindOneWithQuery(string query, object param);
        IEnumerable<TEntity> GetWithQuery(string query, object param);
        Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>> GetWithQuery<TEntity2>(string query, object param);
        Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>> GetWithQuery<TEntity2, TEntity3>(string query, object param);
        PaginationResult<TEntity> GetWithPaging(GetWithPagingParams options);

        #endregion


        #region Synchronous Methods (Store Procedure)

        void ExecuteStoreProcedure(string storeProcedureName, object param);
        void ExecuteManyStoreProcedures(List<string> storeProceduresName, IEnumerable<object> parameters);
        TEntity FindOneWithStoreProcedure(string storeProcedureName, object param);
        IEnumerable<TEntity> GetWithStoreProcedure(string storeProcedureName, object param);
        Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>> GetWithStoreProcedure<TEntity2>(string storeProcedureName,
            object param);
        Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>> GetWithStoreProcedure<TEntity2, TEntity3>(
            string storeProcedureName, object param);

        #endregion


        #region Asynchronous Methods (Query)

        Task ExecuteQueryAsync(string query, object param);
        Task ExecuteManyQueriesAsync(List<string> queries, IEnumerable<object> parameters);
        Task InsertAsync(TEntity entity, bool containsId = false);
        Task InsertManyAsync(IEnumerable<TEntity> entities, bool containsId = false);
        Task ReplaceAsync(TEntity entity);
        Task ReplaceAsync(TEntity entity, ConditionParams conditionParams);
        Task ReplaceManyAsync(IEnumerable<TEntity> entities);
        Task ReplaceManyAsync(IEnumerable<TEntity> entities, List<ConditionParams> customConditions);
        Task UpdateAsync(object param);
        Task UpdateAsync(object param, ConditionParams customCondition);
        Task UpdateManyAsync(IEnumerable<object> param);
        Task UpdateManyAsync(IEnumerable<object> param, List<ConditionParams> customConditions);
        Task DeleteAsync(object id);
        Task DeleteAsync(ConditionParams customCondition);
        Task DeleteManyAsync(IEnumerable<object> ids);
        Task<TEntity> FindOneWithQueryAsync(string query, object param);
        Task<IEnumerable<TEntity>> GetWithQueryAsync(string query, object param);
        Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>> GetWithQueryAsync<TEntity2>(string query, object param);
        Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>> GetWithQueryAsync<TEntity2, TEntity3>(
            string query, object param);
        Task<PaginationResult<TEntity>> GetWithPagingAsync(GetWithPagingParams options);

        #endregion


        #region Asynchronous Methods (Store Procedure)

        Task ExecuteStoreProcedureAsync(string storeProcedureName, object param);
        Task ExecuteManyStoreProceduresAsync(List<string> storeProceduresName, IEnumerable<object> parameters);
        Task<TEntity> FindOneWithStoreProcedureAsync(string storeProcedureName, object param);
        Task<IEnumerable<TEntity>> GetWithStoreProcedureAsync(string storeProcedureName, object param);
        Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>> GetWithStoreProcedureAsync<TEntity2>(string storeProcedureName,
            object param);
        Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>> GetWithStoreProcedureAsync<TEntity2, TEntity3>(
            string storeProcedureName, object param);

        #endregion
    }
}