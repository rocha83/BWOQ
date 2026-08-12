using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rochas.DapperRepository.Specification.Enums;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Tradutor do modo SQL ANSI (modo 2): a inteligência do parser BWOQ fundida
    /// com a composição de comandos SQL multi-dialeto da ORM Dapper.
    /// Devolve APENAS a string SQL traduzida — nunca executa.
    /// </summary>
    public static class BwoqSqlComposer
    {
        public static string Build<T>(BwoqQuery<T> query, DatabaseEngine engine) where T : class
        {
            var entityType = typeof(T);
            var select = query.SelectExpression != null ? BwoqExpression.ParsePredicate(query.SelectExpression) : null;
            var order = query.OrderExpression != null ? BwoqExpression.ParsePredicate(query.OrderExpression) : null;
            var group = query.GroupExpression != null ? BwoqExpression.ParsePredicate(query.GroupExpression) : null;
            var groupBy = query.GroupByExpression != null ? BwoqExpression.ParsePredicate(query.GroupByExpression) : null;
            var criteriaList = query.WhereExpressions.Select(BwoqExpression.ParseCriteria).ToList();

            var table = BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetTableName(entityType), engine);

            // Navegação '>' é exclusiva de composição; sem metadata pública de JOIN a lib
            // orienta os modos LINQ / Repositório(loadComposition).
            EnsureNoNavigation(select, "projeção");
            EnsureNoNavigation(order, "ordenação");
            EnsureNoNavigation(group, "agregação");
            EnsureNoNavigation(groupBy, "agrupamento");
            foreach (var criteria in criteriaList)
                EnsureNoNavigation(criteria.Predicate, "critério");

            var sql = new StringBuilder();
            sql.Append("SELECT ");

            if (group != null)
            {
                if (groupBy == null || groupBy.RootMask == 0)
                    throw new InvalidGroupExpression();

                var groupColumns = BwoqExpression.ResolveRootProps(entityType, groupBy.RootMask);
                var aggregationColumns = BwoqExpression.ResolveRootProps(entityType, group.RootMask);

                if (group.AggregationSuffix == null && aggregationColumns.Length == 0)
                {
                    // Agrupamento sem agregação → DISTINCT das colunas de agrupamento
                    AppendColumns(sql, groupColumns, engine);
                }
                else
                {
                    var columns = new List<string>();
                    columns.AddRange(groupColumns.Select(p => BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(p), engine)));

                    if (aggregationColumns.Length == 0 && group.AggregationSuffix != null)
                        throw new InvalidGroupExpression();

                    foreach (var prop in aggregationColumns)
                    {
                        var column = BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(prop), engine);
                        var (function, alias) = BwoqColumnResolver.BuildAggregation(group.AggregationSuffix.Value, column, prop.Name);
                        var aggAlias = BwoqColumnResolver.QuoteIdentifier(alias, engine);
                        columns.Add(string.Concat(function, "(", column, ") AS ", aggAlias));
                    }

                    sql.Append(string.Join(", ", columns));
                }
            }
            else if (select != null && select.RootMask != 0)
            {
                AppendColumns(sql, BwoqExpression.ResolveRootProps(entityType, select.RootMask), engine);
            }
            else
            {
                sql.Append("*");
            }

            sql.Append(" FROM ").Append(table);

            if (criteriaList.Count > 0)
            {
                var clauses = criteriaList.Select(c => BuildCriteriaClause(entityType, c, engine)).Where(s => !string.IsNullOrEmpty(s));
                var full = string.Join(" AND ", clauses);
                if (!string.IsNullOrEmpty(full))
                    sql.Append(" WHERE ").Append(full);
            }

            if (group != null && groupBy != null && groupBy.RootMask != 0)
            {
                var groupColumns = BwoqExpression.ResolveRootProps(entityType, groupBy.RootMask)
                    .Select(p => BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(p), engine));
                sql.Append(" GROUP BY ").Append(string.Join(", ", groupColumns));
            }

            if (order != null && order.RootMask != 0)
            {
                var orderColumns = BwoqExpression.ResolveRootProps(entityType, order.RootMask)
                    .Select(p => BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(p), engine));
                sql.Append(" ORDER BY ").Append(string.Join(", ", orderColumns))
                   .Append(query.OrderDescending ? " DESC" : " ASC");
            }

            return sql.ToString();
        }

        private static void AppendColumns(StringBuilder sql, System.Reflection.PropertyInfo[] props, DatabaseEngine engine)
        {
            var columns = props
                .Select(p => BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(p), engine))
                .ToList();
            sql.Append(columns.Count > 0 ? string.Join(", ", columns) : "*");
        }

        private static void EnsureNoNavigation(BwoqPredicate predicate, string context)
        {
            if (predicate != null && predicate.HasNavigation)
                throw new BwoqCapabilityException(
                    $"A navegação em composição (token '>') presente em '{context}' não pode ser traduzida para " +
                    "SQL ANSI: a Specification pública (1.5.2) não expõe os metadados de JOIN " +
                    "([RelationalColumn]/[RelatedEntity]). Use o modo LINQ (Apply) ou o modo " +
                    "Repositório (ToRepositoryQuery, composição via loadComposition/[RelatedEntity]).");
        }

        private static string BuildCriteriaClause(Type entityType, BwoqCriteria criteria, DatabaseEngine engine)
        {
            var targets = BwoqExpression.ResolveRootProps(entityType, criteria.Predicate.RootMask);
            if (targets.Length == 0)
                throw new InvalidCriteriaExpression();

            var parts = targets.Select(p =>
            {
                var column = BwoqColumnResolver.QuoteIdentifier(BwoqColumnResolver.GetColumnName(p), engine);
                return BwoqColumnResolver.BuildComparison(column, p.PropertyType, criteria.RawValue, criteria.Operator, engine);
            }).ToList();

            var combinator = criteria.IsAnd ? " AND " : " OR ";
            return string.Concat("(", string.Join(combinator, parts), ")");
        }
    }
}