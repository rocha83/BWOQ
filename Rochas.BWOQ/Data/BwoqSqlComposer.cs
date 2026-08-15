using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rochas.Data.Specification.Enums;
using Rochas.SqlWrapper.Helpers;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Tradutor do modo SQL ANSI (modo 2): a inteligência do parser BWOQ materializa
    /// a expressão como entidade-filtro tipada T e delega a tradução SQL multi-dialeto
    /// para a camada compartilhada Rochas.SqlWrapper (EntitySqlParser).
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

            // Navegação '>' é exclusiva de composição; sem metadata pública de JOIN a lib
            // orienta os modos LINQ / Repositório(loadComposition).
            EnsureNoNavigation(select, "projeção");
            EnsureNoNavigation(order, "ordenação");
            EnsureNoNavigation(group, "agregação");
            EnsureNoNavigation(groupBy, "agrupamento");
            foreach (var criteria in criteriaList)
                EnsureNoNavigation(criteria.Predicate, "critério");

            // Grupo exige o agrupamento ('by') declarado.
            if (group != null && (groupBy == null || groupBy.RootMask == 0))
                throw new InvalidGroupExpression();

            // Materializa a expressão como comando de repositório (filtro tipado + agregações)
            // pelo mesmo caminho do modo 3 — a única inteligência SQL que resta daqui
            // delega para o EntitySqlParser da Rochas.SqlWrapper.
            var repository = BwoqRepositoryComposer.Build(query);

            string showAttributes = null;
            if (select != null && select.RootMask != 0)
                showAttributes = string.Join(",", BwoqExpression.ResolveRootProps(entityType, select.RootMask)
                    .Select(p => p.Name));

            string groupAttributes = null;
            if (groupBy != null && groupBy.RootMask != 0)
                groupAttributes = string.Join(",", BwoqExpression.ResolveRootProps(entityType, groupBy.RootMask)
                    .Select(p => p.Name));

            string sortAttributes = null;
            if (order != null && order.RootMask != 0)
                sortAttributes = string.Join(",", BwoqExpression.ResolveRootProps(entityType, order.RootMask)
                    .Select(p => p.Name));

            var sqlParameters = new Dictionary<string, object>();
            var sql = EntitySqlParser.ParseEntity(repository.Filter, engine, PersistenceAction.Query, repository.Filter,
                filterConjunction: repository.FilterConjunction,
                onlyListableAttributes: select != null && select.RootMask != 0,
                showAttributes: showAttributes,
                groupAttributes: groupAttributes,
                sortAttributes: sortAttributes,
                orderDescending: query.OrderDescending,
                sqlParameters: sqlParameters,
                aggregates: repository.Aggregates != null
                    ? new Dictionary<string, DataAggregationType>(repository.Aggregates)
                    : null);

            return InlineParameters(sql, sqlParameters);
        }

        /// <summary>
        /// O modo ToSql devolve uma string SQL autônoma (nunca executa), então os
        /// marcadores @pN produzidos pelo parser para filtros LIKE/string são
        /// substituídos pelo respectivo literal formatado para o dialeto.
        /// </summary>
        private static string InlineParameters(string sql, Dictionary<string, object> sqlParameters)
        {
            foreach (var param in sqlParameters)
                sql = sql.Replace(param.Key, FormatInlineLiteral(param.Value));

            return sql;
        }

        private static string FormatInlineLiteral(object value)
        {
            if (value == null)
                return "NULL";

            var strValue = value.ToString();
            return string.Concat("'", strValue.Replace("'", "''"), "'");
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
    }
}