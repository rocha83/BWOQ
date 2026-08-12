using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rochas.DapperRepository.Specification.Enums;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Wrapper de consultas na sintaxe BWOQ com três modos de execução, dependendo
    /// apenas de Rochas.DapperRepository.Specification:
    ///  - Apply(IQueryable)      → modo LINQ (fonte do caller);
    ///  - ToSql(DatabaseEngine)  → SQL ANSI puro em string (nunca executa);
    ///  - ToRepositoryQuery()    → comando por reflexão p/ GenericRepository.
    /// O token '>' é exclusivamente navegação em composição (agregados/subclasses).
    /// </summary>
    public sealed class BwoqQuery<T> where T : class
    {
        private readonly List<string> _whereExpressions = new List<string>();

        public string SelectExpression { get; private set; }
        public IReadOnlyList<string> WhereExpressions => _whereExpressions;
        public string OrderExpression { get; private set; }
        public bool OrderDescending { get; private set; }
        public string GroupExpression { get; private set; }
        public string GroupByExpression { get; private set; }

        public static BwoqQuery<T> Create()
        {
            return new BwoqQuery<T>();
        }

        /// <summary>Projeção (Q): máscara raiz e navegação opcional >ordinal:máscara.</summary>
        public BwoqQuery<T> Select(string expression)
        {
            BwoqExpression.ParsePredicate(expression);
            SelectExpression = expression;
            return this;
        }

        /// <summary>Critério (W): mascara::valor[&][operador]. Pode ser encadeado (AND entre cláusulas).</summary>
        public BwoqQuery<T> Where(string expression)
        {
            BwoqExpression.ParseCriteria(expression);
            _whereExpressions.Add(expression);
            return this;
        }

        /// <summary>Ordenação ascendente (O).</summary>
        public BwoqQuery<T> OrderBy(string expression)
        {
            BwoqExpression.ParsePredicate(expression);
            OrderExpression = expression;
            OrderDescending = false;
            return this;
        }

        /// <summary>Ordenação descendente (OD).</summary>
        public BwoqQuery<T> OrderByDescending(string expression)
        {
            BwoqExpression.ParsePredicate(expression);
            OrderExpression = expression;
            OrderDescending = true;
            return this;
        }

        /// <summary>Agrupamento (G): colunas agregadas (opcional sufixo *^~+-) e colunas de grupo (by).</summary>
        public BwoqQuery<T> GroupBy(string groupExpression, string byExpression)
        {
            BwoqExpression.ParsePredicate(groupExpression);
            BwoqExpression.ParsePredicate(byExpression);
            GroupExpression = groupExpression;
            GroupByExpression = byExpression;
            return this;
        }

        #region Modo 1 — LINQ (fonte do caller)

        /// <summary>Aplica a expressão BWOQ sobre a fonte IQueryable fornecida pelo caller.</summary>
        public IQueryable Apply(IQueryable<T> source)
        {
            var engine = new BitWiseQuery<T>(source);
            var filter = engine.Q(SelectExpression ?? AllMaskExpression());

            foreach (var where in _whereExpressions)
                filter = filter.W(where);

            if (OrderExpression != null)
                filter = OrderDescending ? filter.OD(OrderExpression) : filter.O(OrderExpression);

            if (GroupExpression != null)
                return filter.G(GroupExpression, GroupByExpression);

            return filter;
        }

        private static string AllMaskExpression()
        {
            var propertyCount = BwoqExpression.GetRootProperties(typeof(T)).Length;
            return ((BigInteger.One << propertyCount) - 1).ToString();
        }

        #endregion

        #region Modo 2 — SQL ANSI (somente string)

        /// <summary>Traduz a expressão para uma única string SQL ANSI (multi-dialeto), sem executar.</summary>
        public string ToSql(DatabaseEngine engine)
        {
            return BwoqSqlComposer.Build(this, engine);
        }

        #endregion

        #region Modo 3 — GenericRepository (reflexão em entidade-filtro)

        /// <summary>
        /// Traduz a expressão para um comando do GenericRepository: entidade-filtro T por
        /// reflexão, [Filterable] Search, [RangeFilter] (>= / <=), loadComposition para
        /// composição e builders de ordenação/agrupamento com DataAggregationType.
        /// </summary>
        public BwoqRepositoryQuery<T> ToRepositoryQuery()
        {
            return BwoqRepositoryComposer.Build(this);
        }

        #endregion
    }
}