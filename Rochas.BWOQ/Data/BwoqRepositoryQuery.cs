using System;
using System.Collections.Generic;
using System.Linq;
using Rochas.Data.Specification.Enums;
using Rochas.Data.Specification.Interfaces;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Artefato do modo GenericRepository (modo 3): expressão BWOQ traduzida para
    /// reflexão em entidade-filtro, Search por [Filterable], composição (loadComposition),
    /// ordenação/agrupamento e agregações via builders da Specification.
    /// A execução permanece sob responsabilidade do caller (await/ToList).
    /// </summary>
    public sealed class BwoqRepositoryQuery<T> where T : class
    {
        public T Filter { get; internal set; }
        public bool UseSearch { get; internal set; }
        public object SearchCriteria { get; internal set; }
        public bool LoadComposition { get; internal set; }
        public bool FilterConjunction { get; internal set; }

        public IReadOnlyList<string> OrderByAttributes { get; internal set; } = Array.Empty<string>();
        public bool OrderDescending { get; internal set; }
        public IReadOnlyList<string> GroupByAttributes { get; internal set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, DataAggregationType> Aggregates { get; internal set; }
            = new Dictionary<string, DataAggregationType>();

        public IQueryBuilder<T> Build(IGenericRepository<T> repository)
        {
            var builder = UseSearch
                ? repository.Search(SearchCriteria, LoadComposition, FilterConjunction)
                : repository.Query(Filter, LoadComposition, FilterConjunction);
            return ApplyShape(builder);
        }

        public IQuerySyncBuilder<T> BuildSync(IGenericRepository<T> repository)
        {
            var builder = UseSearch
                ? repository.SearchSync(SearchCriteria, LoadComposition, FilterConjunction)
                : repository.QuerySync(Filter, LoadComposition, FilterConjunction);
            return ApplyShape(builder);
        }

        public IQueryPaginatedBuilder<T> Build(IGenericRepository<T> repository, int page, int pageSize)
        {
            var builder = UseSearch
                ? repository.Search(SearchCriteria, page, pageSize, LoadComposition, FilterConjunction)
                : repository.Query(Filter, page, pageSize, LoadComposition, FilterConjunction);
            return ApplyShape(builder);
        }

        public IQueryPaginatedBuilder<T> BuildSync(IGenericRepository<T> repository, int page, int pageSize)
        {
            var builder = UseSearch
                ? repository.SearchSync(SearchCriteria, page, pageSize, LoadComposition, FilterConjunction)
                : repository.QuerySync(Filter, page, pageSize, LoadComposition, FilterConjunction);
            return ApplyShape(builder);
        }

        private IQueryBuilder<T> ApplyShape(IQueryBuilder<T> builder)
        {
            if (GroupByAttributes.Count > 0)
                return Aggregates.Count > 0
                    ? builder.GroupBy(GroupByAttributes.ToArray(), new Dictionary<string, DataAggregationType>(Aggregates))
                    : builder.GroupBy(GroupByAttributes.ToArray());
            if (OrderByAttributes.Count > 0)
                return OrderDescending
                    ? builder.OrderByDescending(OrderByAttributes.ToArray())
                    : builder.OrderBy(OrderByAttributes.ToArray());
            return builder;
        }

        private IQuerySyncBuilder<T> ApplyShape(IQuerySyncBuilder<T> builder)
        {
            if (GroupByAttributes.Count > 0)
                return Aggregates.Count > 0
                    ? builder.GroupBy(GroupByAttributes.ToArray(), new Dictionary<string, DataAggregationType>(Aggregates))
                    : builder.GroupBy(GroupByAttributes.ToArray());
            if (OrderByAttributes.Count > 0)
                return OrderDescending
                    ? builder.OrderByDescending(OrderByAttributes.ToArray())
                    : builder.OrderBy(OrderByAttributes.ToArray());
            return builder;
        }

        private IQueryPaginatedBuilder<T> ApplyShape(IQueryPaginatedBuilder<T> builder)
        {
            if (GroupByAttributes.Count > 0)
                return Aggregates.Count > 0
                    ? builder.GroupBy(GroupByAttributes.ToArray(), new Dictionary<string, DataAggregationType>(Aggregates))
                    : builder.GroupBy(GroupByAttributes.ToArray());
            if (OrderByAttributes.Count > 0)
                return OrderDescending
                    ? builder.OrderByDescending(OrderByAttributes.ToArray())
                    : builder.OrderBy(OrderByAttributes.ToArray());
            return builder;
        }
    }
}