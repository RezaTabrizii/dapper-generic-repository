using System.Data;
using Dapper;
using static Dapper.SqlMapper;
using DapperGenericRepository.Extensions;
using DapperGenericRepository.Contracts;
using DapperGenericRepository.Models.Parameters;
using DapperGenericRepository.Models.Results;
using Pluralize.NET;
using System.Text;
using DapperGenericRepository.Utilities;

namespace DapperGenericRepository.Repository
{
    public abstract class DapperBaseRepository<TEntity> : IDapperBaseRepository<TEntity>
        where TEntity : class
    {
        #region Constructor

        private readonly string _tableName;
        private readonly ISqlConnection _sqlConnection;
        private readonly IEnumerable<string> _allColumnsName;
        private readonly IEnumerable<string> _filteredColumnsName;
        public DapperBaseRepository(ISqlConnection sqlConnection)
        {
            Pluralizer pluralizer = new();
            _tableName = pluralizer.Pluralize(typeof(TEntity).Name);
            _sqlConnection = sqlConnection;

            var columns = sqlConnection.GetColumnsName(_tableName);
            _allColumnsName = columns;
            _filteredColumnsName = columns.Where(q => !string.Equals(q, "CreatedMoment", StringComparison.CurrentCultureIgnoreCase)
                && !string.Equals(q, "ModifiedMoment", StringComparison.CurrentCultureIgnoreCase));

            Initialize();
        }

        #endregion


        #region Synchronous Methods (Query)

        protected virtual void Initialize()
        {
        }

        public void ExecuteQuery(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                dbConnection.Execute(query, dynamicParameters, commandType: CommandType.Text);
            }
        }

        public void ExecuteManyQueries(List<string> queries, IEnumerable<object> parameters)
        {
            var parametersList = parameters.ToList();

            using (var connection = _sqlConnection.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var query in queries)
                        {
                            var param = parameters is not null && parametersList.Count >= 1
                                ? parametersList[queries.IndexOf(query)]
                                : null;
                            DynamicParameters dynamicParameters = param is not null
                                ? param.ToDynamicParameters()
                                : null;

                            connection.Execute(query, dynamicParameters, commandType: CommandType.Text, transaction: transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Insert(TEntity entity, bool containsId = false)
        {
            var columnsName = containsId ? _filteredColumnsName : _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var columns = string.Join(", ", columnsName);
            var parameters = string.Join(", ", columnsName.Select(e => $"@{e}"));
            var query = $"INSERT INTO {_tableName} ({columns}, CreatedMoment) VALUES ({parameters}, '{DateTime.UtcNow}')";

            ExecuteQuery(query, entity);
        }

        public void InsertMany(IEnumerable<TEntity> entities, bool containsId = false)
        {
            var columnsName = containsId ? _filteredColumnsName
                : _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));
            var columns = string.Join(", ", columnsName);

            var queryBuilder = new StringBuilder($"INSERT INTO {_tableName} ({columns}, CreatedMoment) VALUES ");

            DynamicParameters parameters = new();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@CreatedMoment_{currentIndex}", DateTime.UtcNow);

                var parameterString = string.Join(", ", columnsName.Select(q => $"@{q}_{currentIndex}")) + $", @CreatedMoment_{currentIndex}";
                queryBuilder.Append($"({parameterString}), ");
            }
            var query = queryBuilder.ToString().TrimEnd(',', ' ');

            ExecuteManyQueries([query], [parameters]);
        }

        public void Replace(TEntity entity)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var columns = string.Join(", ", columnsName.Select(e => $"{e} = @{e}"));
            var query = $"UPDATE {_tableName} SET ({columns}, ModifiedMoment = {DateTime.UtcNow}) WHERE Id = @Id";

            ExecuteQuery(query, entity);
        }

        public void Replace(TEntity entity, ConditionParams customCondition)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}"));
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' {condition.WhereClause}";

            var param = entity.ToDynamicParameters();
            param.AddDynamicParams(condition.Parameters);
            ExecuteQuery(query, param);
        }

        public void ReplaceMany(IEnumerable<TEntity> entities)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@Id_{currentIndex}", typeof(TEntity).GetProperty("Id").GetValue(entity));
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public void ReplaceMany(IEnumerable<TEntity> entities, List<ConditionParams> customConditions)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var condition = SqlQueryHelper.GenerateWhereClause(customConditions[currentIndex], _allColumnsName, currentIndex);
                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} {condition.WhereClause}");

                parameters.AddDynamicParams(condition.Parameters);
                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public void Update(object param)
        {
            var columnsName = _filteredColumnsName.Where(q => param.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

            var columns = string.Join(", ", columnsName.Select(e => $"{e} = @{e}"));
            var query = $"UPDATE {_tableName} SET ({columns}, ModifiedMoment = {DateTime.UtcNow}) WHERE Id = @Id";

            ExecuteQuery(query, param);
        }

        public void Update(object param, ConditionParams customCondition)
        {
            var columnsName = _filteredColumnsName.Where(q => param.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

            var setClause = string.Join(", ", columnsName.Select(e => $"{e} = @{e}"));
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' {condition.WhereClause}";

            var parameters = param.ToDynamicParameters();
            parameters.AddDynamicParams(condition.Parameters);
            ExecuteQuery(query, parameters);
        }

        public void UpdateMany(IEnumerable<object> param)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in param.Select((entity, index) => (entity, index)))
            {
                var columnsName = _filteredColumnsName.Where(q => q != "Id" && entity.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

                var parameters = new DynamicParameters();

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@Id_{currentIndex}", typeof(TEntity).GetProperty("Id").GetValue(entity));
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public void UpdateMany(IEnumerable<object> param, List<ConditionParams> customConditions)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in param.Select((entity, index) => (entity, index)))
            {
                var columnsName = _filteredColumnsName.Where(q => entity.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

                var parameters = new DynamicParameters();

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var condition = SqlQueryHelper.GenerateWhereClause(customConditions[currentIndex], _allColumnsName, currentIndex);
                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} {condition.WhereClause}");

                parameters.AddDynamicParams(condition.Parameters);
                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public void Delete(object id)
        {
            var query = $"DELETE FROM {_tableName} WHERE Id = @Id";

            ExecuteQuery(query, id);
        }

        public void Delete(ConditionParams customCondition)
        {
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"DELETE FROM {_tableName} {condition.WhereClause}";

            ExecuteQuery(query, condition.Parameters);
        }

        public void DeleteMany(IEnumerable<object> ids)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();

            foreach (var (id, currentIndex) in ids.Select((id, index) => (id, index)))
            {
                var parameters = new DynamicParameters();
                parameters.Add($"@Id_{currentIndex}", id);
                var queryBuilder = new StringBuilder($"DELETE FROM {_tableName} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public void DeleteMany(List<ConditionParams> customConditions)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();

            foreach (var (customCondition, currentIndex) in customConditions.Select((customCondition, index) => (customCondition, index)))
            {
                var condition = SqlQueryHelper.GenerateWhereClause(customConditions[currentIndex], _allColumnsName, currentIndex);
                var queryBuilder = new StringBuilder($"DELETE FROM {_tableName} {condition.WhereClause}");

                parameterList.Add(condition.Parameters);
                queries.Add(queryBuilder.ToString());
            }

            ExecuteManyQueries(queries, parameterList);
        }

        public TEntity FindOneWithQuery(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                return dbConnection.QueryFirstOrDefault<TEntity>(query, dynamicParameters, commandType: CommandType.Text);
            }
        }

        public IEnumerable<TEntity> GetWithQuery(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            IEnumerable<TEntity> result = [];
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = dbConnection.Query<TEntity>(query, dynamicParameters, commandType: CommandType.Text);
            }

            return result ?? ([]);
        }

        public Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>> GetWithQuery<TEntity2>(string query, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(query, param, commandType: CommandType.Text);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
            }

            return (item1 != null && item2 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(item1, item2)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(new List<TEntity>(), new List<TEntity2>());
        }

        public Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>> GetWithQuery<TEntity2, TEntity3>(string query, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            IEnumerable<TEntity3> item3;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(query, param, commandType: CommandType.Text);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
                item3 = data.Read<TEntity3>();
            }

            return (item1 != null && item2 != null && item3 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(item1, item2, item3)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(new List<TEntity>(),
                    new List<TEntity2>(), new List<TEntity3>());
        }

        public PaginationResult<TEntity> GetWithPaging(GetWithPagingParams options)
        {
            if ((options.ConditionLeftSide.Count >= 1 && !options.ConditionLeftSide.All(_allColumnsName.Contains))
                || (!string.IsNullOrEmpty(options.OrderColumnName) && !_allColumnsName.Contains(options.OrderColumnName)))
                throw new Exception("Columns name are invalid");

            DynamicParameters dynamicParameters = options.Parameters.ToDynamicParameters();

            var query = options.SqlQuery;
            query.ApplyFilter(ref dynamicParameters, options.ConditionLeftSide, options.ComparisonOperators,
                options.ConditionRightSide, out query, options.LogicOperators);

            query.ApplySorting(options.OrderColumnName, options.OrderByDescending ?? true, out query);

            query.ApplyPaging(ref dynamicParameters, options.Page, options.PageSize, out query);

            IEnumerable<TEntity> result;
            long totalDataCount;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(options.SqlQuery, dynamicParameters, commandType: CommandType.Text);
                result = data.Read<TEntity>();
                totalDataCount = data.Read<int>().Single();
            }

            return result.ToPaginationResult(totalDataCount, options.PageSize);
        }

        #endregion


        #region Synchronous Methods (Store Procedure)

        public void ExecuteStoreProcedure(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                dbConnection.Execute(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }
        }

        public void ExecuteManyStoreProcedures(List<string> storeProceduresName, IEnumerable<object> parameters)
        {
            var parametersList = parameters.ToList();

            using (var connection = _sqlConnection.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var storeProcedureName in storeProceduresName)
                        {
                            var param = parameters is not null && parametersList.Count >= 1
                                ? parametersList[storeProceduresName.IndexOf(storeProcedureName)]
                                : null;
                            DynamicParameters dynamicParameters = param is not null
                                ? param.ToDynamicParameters()
                                : null;

                            connection.Execute(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure, transaction: transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public TEntity FindOneWithStoreProcedure(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            TEntity result;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = dbConnection.QueryFirstOrDefault<TEntity>(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }

            return result;
        }

        public IEnumerable<TEntity> GetWithStoreProcedure(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            IEnumerable<TEntity> result = [];
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = dbConnection.Query<TEntity>(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }

            return result ?? ([]);
        }

        public Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>> GetWithStoreProcedure<TEntity2>(string storeProcedureName,
            object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(storeProcedureName, param, commandType: CommandType.StoredProcedure);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
            }

            return (item1 != null && item2 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(item1, item2)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(new List<TEntity>(), new List<TEntity2>());
        }

        public Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>> GetWithStoreProcedure<TEntity2, TEntity3>(
            string storeProcedureName, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            IEnumerable<TEntity3> item3;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(storeProcedureName, param, commandType: CommandType.StoredProcedure);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
                item3 = data.Read<TEntity3>();
            }

            return (item1 != null && item2 != null && item3 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(item1, item2, item3)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(new List<TEntity>(),
                    new List<TEntity2>(), new List<TEntity3>());
        }

        #endregion


        #region Asynchronous Methods (Query)

        public async Task ExecuteQueryAsync(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null
                ? param.ToDynamicParameters()
                : null;

            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                await dbConnection.ExecuteAsync(query, dynamicParameters, commandType: CommandType.Text);
            }
        }

        public async Task ExecuteManyQueriesAsync(List<string> queries, IEnumerable<object> parameters)
        {
            var parametersList = parameters.ToList();

            using (var connection = _sqlConnection.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var query in queries)
                        {
                            var param = parameters is not null && parametersList.Count >= 1
                                ? parametersList[queries.IndexOf(query)]
                                : null;
                            DynamicParameters dynamicParameters = param is not null
                                ? param.ToDynamicParameters()
                                : null;

                            await connection.ExecuteAsync(query, dynamicParameters, commandType: CommandType.Text, transaction: transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task InsertAsync(TEntity entity, bool containsId = false)
        {
            var columnsName = containsId ? _filteredColumnsName : _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var columns = string.Join(", ", columnsName);
            var parameters = string.Join(", ", columnsName.Select(q => $"@{q}"));
            var query = $"INSERT INTO {_tableName} ({columns}, CreatedMoment) VALUES ({parameters}, '{DateTime.UtcNow}')";

            await ExecuteQueryAsync(query, entity);
        }

        public async Task InsertManyAsync(IEnumerable<TEntity> entities, bool containsId = false)
        {
            var columnsName = containsId ? _filteredColumnsName
                : _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));
            var columns = string.Join(", ", columnsName);

            var queryBuilder = new StringBuilder($"INSERT INTO {_tableName} ({columns}, CreatedMoment) VALUES ");

            DynamicParameters parameters = new();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@CreatedMoment_{currentIndex}", DateTime.UtcNow);

                var parameterString = string.Join(", ", columnsName.Select(q => $"@{q}_{currentIndex}")) + $", @CreatedMoment_{currentIndex}";
                queryBuilder.Append($"({parameterString}), ");
            }
            var query = queryBuilder.ToString().TrimEnd(',', ' ');

            await ExecuteManyQueriesAsync([query], [parameters]);
        }

        public async Task ReplaceAsync(TEntity entity)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}"));
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' WHERE Id = @Id";

            await ExecuteQueryAsync(query, entity);
        }

        public async Task ReplaceAsync(TEntity entity, ConditionParams customCondition)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}"));
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' {condition.WhereClause}";

            var param = entity.ToDynamicParameters();
            param.AddDynamicParams(condition.Parameters);
            await ExecuteQueryAsync(query, param);
        }

        public async Task ReplaceManyAsync(IEnumerable<TEntity> entities)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();
                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@Id_{currentIndex}", typeof(TEntity).GetProperty("Id").GetValue(entity));
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            await ExecuteManyQueriesAsync(queries, parameterList);
        }

        public async Task ReplaceManyAsync(IEnumerable<TEntity> entities, List<ConditionParams> customConditions)
        {
            var columnsName = _filteredColumnsName.Where(q => !string.Equals(q, "Id", StringComparison.CurrentCultureIgnoreCase));

            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in entities.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();
                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", typeof(TEntity).GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var condition = SqlQueryHelper.GenerateWhereClause(customConditions[currentIndex], _allColumnsName, currentIndex);
                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} {condition.WhereClause}");

                parameters.AddDynamicParams(condition.Parameters);
                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            await ExecuteManyQueriesAsync(queries, parameterList);
        }

        public async Task UpdateAsync(object param)
        {
            var columnsName = _filteredColumnsName.Where(q => q != "Id" && param.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

            var setClause = string.Join(", ", columnsName.Select(e => $"{e} = @{e}"));
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' WHERE Id = @Id";

            await ExecuteQueryAsync(query, param);
        }

        public async Task UpdateAsync(object param, ConditionParams customCondition)
        {
            var columnsName = _filteredColumnsName.Where(q => param.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

            var setClause = string.Join(", ", columnsName.Select(e => $"{e} = @{e}"));
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"UPDATE {_tableName} SET {setClause}, ModifiedMoment = '{DateTime.UtcNow}' {condition.WhereClause}";

            var parameters = param.ToDynamicParameters();
            parameters.AddDynamicParams(condition.Parameters);
            await ExecuteQueryAsync(query, parameters);
        }

        public async Task UpdateManyAsync(IEnumerable<object> param)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in param.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();
                var columnsName = _filteredColumnsName.Where(q => q != "Id" && entity.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", entity.GetType().GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@Id_{currentIndex}", entity.GetType().GetProperty("Id").GetValue(entity));
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            await ExecuteManyQueriesAsync(queries, parameterList);
        }

        public async Task UpdateManyAsync(IEnumerable<object> param, List<ConditionParams> customConditions)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();
            foreach (var (entity, currentIndex) in param.Select((entity, index) => (entity, index)))
            {
                var parameters = new DynamicParameters();
                var columnsName = _filteredColumnsName.Where(q => entity.GetType().GetProperties()
                    .Any(p => string.Equals(q, p.Name, StringComparison.CurrentCultureIgnoreCase)));

                foreach (var columnName in columnsName)
                {
                    parameters.Add($"@{columnName}_{currentIndex}", entity.GetType().GetProperty(columnName).GetValue(entity));
                }
                parameters.Add($"@ModifiedMoment_{currentIndex}", DateTime.UtcNow);

                var condition = SqlQueryHelper.GenerateWhereClause(customConditions[currentIndex], _allColumnsName, currentIndex);
                var setClause = string.Join(", ", columnsName.Select(q => $"{q} = @{q}_{currentIndex}")) + $", ModifiedMoment = @ModifiedMoment_{currentIndex}";
                var queryBuilder = new StringBuilder($"UPDATE {_tableName} SET {setClause} {condition.WhereClause}");

                parameters.AddDynamicParams(condition.Parameters);
                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            await ExecuteManyQueriesAsync(queries, parameterList);
        }

        public async Task DeleteAsync(object id)
        {
            var query = $"DELETE FROM {_tableName} WHERE Id = @Id";

            await ExecuteQueryAsync(query, id);
        }

        public async Task DeleteAsync(ConditionParams customCondition)
        {
            var condition = SqlQueryHelper.GenerateWhereClause(customCondition, _allColumnsName);
            var query = $"DELETE FROM {_tableName} {condition.WhereClause}";

            await ExecuteQueryAsync(query, condition.Parameters);
        }

        public async Task DeleteManyAsync(IEnumerable<object> ids)
        {
            var queries = new List<string>();
            var parameterList = new List<DynamicParameters>();

            foreach (var (id, currentIndex) in ids.Select((id, index) => (id, index)))
            {
                var parameters = new DynamicParameters();
                parameters.Add($"@Id_{currentIndex}", id);
                var queryBuilder = new StringBuilder($"DELETE FROM {_tableName} WHERE Id = @Id_{currentIndex}");

                parameterList.Add(parameters);
                queries.Add(queryBuilder.ToString());
            }

            await ExecuteManyQueriesAsync(queries, parameterList);
        }

        public async Task<TEntity> FindOneWithQueryAsync(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            TEntity result;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = await dbConnection.QueryFirstOrDefaultAsync<TEntity>(query, dynamicParameters, commandType: CommandType.Text);
            }

            return result;
        }

        public async Task<IEnumerable<TEntity>> GetWithQueryAsync(string query, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            IEnumerable<TEntity> result = [];
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = await dbConnection.QueryAsync<TEntity>(query, dynamicParameters, commandType: CommandType.Text);
            }

            return result ?? ([]);
        }

        public async Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>> GetWithQueryAsync<TEntity2>(string query, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(query, param, commandType: CommandType.Text);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
            }

            return (item1 != null && item2 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(item1, item2)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(new List<TEntity>(), new List<TEntity2>());
        }

        public async Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>> GetWithQueryAsync<TEntity2, TEntity3>(
            string query, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            IEnumerable<TEntity3> item3;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(query, param, commandType: CommandType.Text);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
                item3 = data.Read<TEntity3>();
            }

            return (item1 != null && item2 != null && item3 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(item1, item2, item3)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(new List<TEntity>(),
                    new List<TEntity2>(), new List<TEntity3>());
        }

        public async Task<PaginationResult<TEntity>> GetWithPagingAsync(GetWithPagingParams options)
        {
            if ((options.ConditionLeftSide.Count >= 1 && !options.ConditionLeftSide.All(_allColumnsName.Contains))
                || (!string.IsNullOrEmpty(options.OrderColumnName) && !_allColumnsName.Contains(options.OrderColumnName)))
                throw new Exception("Columns name are invalid");

            DynamicParameters dynamicParameters = options.Parameters.ToDynamicParameters();

            var query = options.SqlQuery;
            query.ApplyFilter(ref dynamicParameters, options.ConditionLeftSide, options.ComparisonOperators,
                options.ConditionRightSide, out query, options.LogicOperators);

            query.ApplySorting(options.OrderColumnName, options.OrderByDescending ?? true, out query);

            query.ApplyPaging(ref dynamicParameters, options.Page, options.PageSize, out query);

            IEnumerable<TEntity> result;
            long totalDataCount;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(query, dynamicParameters, commandType: CommandType.Text);
                result = data.Read<TEntity>();
                totalDataCount = data.Read<int>().Single();
            }

            return result.ToPaginationResult(totalDataCount, options.PageSize);
        }

        #endregion


        #region Asynchronous Methods (Store Procedure)

        public async Task ExecuteStoreProcedureAsync(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null
                ? param.ToDynamicParameters()
                : null;

            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                await dbConnection.ExecuteAsync(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task ExecuteManyStoreProceduresAsync(List<string> storeProceduresName, IEnumerable<object> parameters)
        {
            var parametersList = parameters.ToList();

            using (var connection = _sqlConnection.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var storeProcedureName in storeProceduresName)
                        {
                            var param = parameters is not null && parametersList.Count >= 1
                                ? parametersList[storeProceduresName.IndexOf(storeProcedureName)]
                                : null;
                            DynamicParameters dynamicParameters = param is not null
                                ? param.ToDynamicParameters()
                                : null;

                            await connection.ExecuteAsync(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure, transaction: transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<TEntity> FindOneWithStoreProcedureAsync(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            TEntity result;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = await dbConnection.QueryFirstOrDefaultAsync<TEntity>(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }

            return result;
        }

        public async Task<IEnumerable<TEntity>> GetWithStoreProcedureAsync(string storeProcedureName, object param)
        {
            DynamicParameters dynamicParameters = param is not null ? param.ToDynamicParameters() : null;

            IEnumerable<TEntity> result = [];
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                result = await dbConnection.QueryAsync<TEntity>(storeProcedureName, dynamicParameters, commandType: CommandType.StoredProcedure);
            }

            return result ?? ([]);
        }

        public async Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>> GetWithStoreProcedureAsync<TEntity2>(string storeProcedureName,
            object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(storeProcedureName, param, commandType: CommandType.StoredProcedure);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
            }

            return (item1 != null && item2 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(item1, item2)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>>(new List<TEntity>(), new List<TEntity2>());
        }

        public async Task<Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>> GetWithStoreProcedureAsync<TEntity2, TEntity3>(
            string storeProcedureName, object param)
        {
            IEnumerable<TEntity> item1;
            IEnumerable<TEntity2> item2;
            IEnumerable<TEntity3> item3;
            using (var dbConnection = _sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(storeProcedureName, param, commandType: CommandType.StoredProcedure);
                item1 = data.Read<TEntity>();
                item2 = data.Read<TEntity2>();
                item3 = data.Read<TEntity3>();
            }

            return (item1 != null && item2 != null && item3 != null)
                ? new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(item1, item2, item3)
                : new Tuple<IEnumerable<TEntity>, IEnumerable<TEntity2>, IEnumerable<TEntity3>>(new List<TEntity>(),
                    new List<TEntity2>(), new List<TEntity3>());
        }

        #endregion
    }
}
