using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Rochas.Data.Specification.Annotations;
using Rochas.Data.Specification.Enums;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Tradutor do modo GenericRepository (modo 3): a inteligência do parser BWOQ
    /// materializa critérios como entidade-filtro tipada T por reflexão, respeitando
    /// a semântica de "valor vazio ignorado" da ORM, e exporta ordenação/agrupamento
    /// para os builders da Rochas.Data.Specification.
    /// </summary>
    public static class BwoqRepositoryComposer
    {
        public static BwoqRepositoryQuery<T> Build<T>(BwoqQuery<T> query) where T : class
        {
            var entityType = typeof(T);
            var result = new BwoqRepositoryQuery<T>
            {
                Filter = Activator.CreateInstance<T>()
            };

            var select = query.SelectExpression != null ? BwoqExpression.ParsePredicate(query.SelectExpression) : null;
            var order = query.OrderExpression != null ? BwoqExpression.ParsePredicate(query.OrderExpression) : null;
            var group = query.GroupExpression != null ? BwoqExpression.ParsePredicate(query.GroupExpression) : null;
            var groupBy = query.GroupByExpression != null ? BwoqExpression.ParsePredicate(query.GroupByExpression) : null;
            var criteriaList = query.WhereExpressions.Select(BwoqExpression.ParseCriteria).ToList();

            // Composição (projeção de agregados) → eager loading via [RelatedEntity].
            result.LoadComposition = select != null && select.HasNavigation;

            ApplyCriteria(entityType, criteriaList, result);

            if (order != null)
            {
                EnsureRootOnly(order, "ordenação");
                result.OrderByAttributes = BwoqExpression.ResolveRootProps(entityType, order.RootMask).Select(p => p.Name).ToArray();
                result.OrderDescending = query.OrderDescending;
            }

            if (group != null || groupBy != null)
            {
                if (groupBy == null || groupBy.RootMask == 0)
                    throw new InvalidGroupExpression();

                EnsureRootOnly(group, "agregação");
                EnsureRootOnly(groupBy, "agrupamento");

                result.GroupByAttributes = BwoqExpression.ResolveRootProps(entityType, groupBy.RootMask).Select(p => p.Name).ToArray();

                var aggregationType = BwoqColumnResolver.ResolveAggregationType(group?.AggregationSuffix);
                if (aggregationType.HasValue && group != null)
                {
                    var aggregation = new Dictionary<string, DataAggregationType>();
                    var props = BwoqExpression.ResolveRootProps(entityType, group.RootMask);
                    if (props.Length == 0 && aggregationType != DataAggregationType.Count)
                        throw new InvalidGroupExpression();

                    foreach (var prop in props)
                        aggregation[prop.Name] = aggregationType.Value;

                    if (aggregation.Count > 0)
                        result.Aggregates = aggregation;
                }
            }

            return result;
        }

        private static void ApplyCriteria<T>(Type entityType, List<BwoqCriteria> criteriaList, BwoqRepositoryQuery<T> result) where T : class
        {
            bool? conjunction = null;

            foreach (var criteria in criteriaList)
            {
                if (criteria.Predicate.HasNavigation)
                    throw new BwoqCapabilityException(
                        "Critério sobre agregado (token '>' em Where) não é expressável pelo GenericRepository: " +
                        "o loadComposition carrega composição para leitura, mas a Specification pública não expõe " +
                        "o metadata de JOIN ([RelationalColumn]) para filtrar por coluna do agregado. " +
                        "Use SQL (ToSql) ou LINQ (Apply).");

                var targets = BwoqExpression.ResolveRootProps(entityType, criteria.Predicate.RootMask);
                if (targets.Length == 0)
                    throw new InvalidCriteriaExpression();

                // Caminho Search: um único critério 'like' sobre coluna única [Filterable], em conjunção.
                if (criteriaList.Count == 1 && targets.Length == 1
                    && criteria.Operator == BwoqOperator.Default
                    && targets[0].PropertyType == typeof(string)
                    && IsFilterable(targets[0]))
                {
                    result.UseSearch = true;
                    result.SearchCriteria = criteria.RawValue;
                    result.FilterConjunction = true;
                    continue;
                }

                // A ORM publica UM único modo de combinação (filterConjunction) para todas as
                // condições do filtro → Disjunção (OR) é expressável se TODA a expressão for
                // disjuntiva; Mistura AND/OR não é combinável.
                foreach (var target in targets)
                {
                    if (conjunction.HasValue && conjunction.Value != criteria.IsAnd)
                        throw new BwoqCapabilityException(
                            "Mistura de conjunção/disjunção (AND e OR) não é expressável pelo GenericRepository: " +
                            "ele aplica um único modo de combinação (filterConjunction) a todas as condições. " +
                            "Use SQL (ToSql) ou LINQ (Apply).");

                    conjunction = criteria.IsAnd;
                }

                foreach (var target in targets)
                    ApplyCriteriaToProperty(entityType, result.Filter, target, criteria);
            }

            if (conjunction.HasValue)
                result.FilterConjunction = conjunction.Value;
        }

        private static void ApplyCriteriaToProperty(Type entityType, object filter, PropertyInfo prop, BwoqCriteria criteria)
        {
            var rawValue = criteria.RawValue;
            var propType = prop.PropertyType;

            if (rawValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                throw new BwoqCapabilityException(
                    $"Critério 'null' sobre '{prop.Name}' não é expressável via entidade-filtro da ORM " +
                    "(valor nulo é ignorado pelo filtro). Use SQL (ToSql) ou LINQ (Apply).");

            switch (criteria.Operator)
            {
                case BwoqOperator.Equal:
                    ApplyEquality(filter, prop, propType, rawValue);
                    break;
                case BwoqOperator.Default:
                    ApplyDefault(filter, prop, propType, rawValue);
                    break;
                case BwoqOperator.GreaterOrEqual:
                case BwoqOperator.LessOrEqual:
                    ApplyRange(filter, entityType, prop, propType, rawValue, criteria.Operator);
                    break;
                default:
                    throw new BwoqCapabilityException(
                        $"Operador relacional estrito ('{criteria.Operator}') sobre '{prop.Name}' não é expressável " +
                        "pelo GenericRepository (só >=/<= via [RangeFilter]). Use SQL (ToSql) ou LINQ (Apply).");
            }
        }

        private static void ApplyEquality(object filter, PropertyInfo prop, Type propType, string rawValue)
        {
            if (propType == typeof(string))
                EqualityOnString(filter, prop, rawValue);
            else
                EqualityOnValue(filter, prop, propType, rawValue);
        }

        private static void ApplyDefault(object filter, PropertyInfo prop, Type propType, string rawValue)
        {
            if (propType == typeof(string))
            {
                // A ORM trata string (não-chave) com LIKE '%valor%' — mesmo comportamento
                // do critério BWOQ sem operador (semelhança 'like').
                prop.SetValue(filter, rawValue);
                return;
            }

            EqualityOnValue(filter, prop, propType, rawValue);
        }

        private static void EqualityOnString(object filter, PropertyInfo prop, string rawValue)
        {
            var isKey = prop.GetCustomAttribute<KeyAttribute>() != null;
            if (isKey)
            {
                // Chave primária: a ORM compara com igualdade.
                var coerced = CoerceValue(rawValue, prop.PropertyType);
                prop.SetValue(filter, coerced);
                return;
            }

            throw new BwoqCapabilityException(
                $"Igualdade exata ('=') sobre string '{prop.Name}' não é expressável pelo GenericRepository " +
                "(strings são comparadas com LIKE/semelhança). Use SQL (ToSql) ou LINQ (Apply), ou marque como chave.");
        }

        private static void EqualityOnValue(object filter, PropertyInfo prop, Type propType, string rawValue)
        {
            var coerced = CoerceValue(rawValue, propType);

            if (propType == typeof(bool))
            {
                if (coerced is bool boolValue && !boolValue)
                    throw new BwoqCapabilityException(
                        $"Filtro booleano 'false' sobre '{prop.Name}' é ignorado pela ORM (valor vazio). Use SQL (ToSql) ou LINQ (Apply).");

                prop.SetValue(filter, coerced);
            }
            else if (!IsEmptyDefault(coerced))
            {
                prop.SetValue(filter, coerced);
            }
            else
            {
                throw new BwoqCapabilityException(
                    $"Valor default ('{rawValue}') sobre '{prop.Name}' é ignorado pela ORM como filtro vazio. " +
                    "Use SQL (ToSql) ou LINQ (Apply).");
            }
        }

private static void ApplyRange(object filter, Type entityType, PropertyInfo prop, Type propType,
                               string rawValue, BwoqOperator op)
        {
            var rangeAttribute = prop.GetCustomAttribute<RangeFilterAttribute>();

            if (op == BwoqOperator.GreaterOrEqual)
            {
                // Limite inferior ("de"): a própria propriedade decorada [RangeFilter] recebe o valor.
                if (rangeAttribute == null)
                    throw new BwoqCapabilityException(
                        $"Comparação '>= ' sobre '{prop.Name}' exige decoração [RangeFilter(LinkedRangeProperty = ...)] " +
                        "para ser expressável pelo GenericRepository.");

                var lowerValue = CoerceValue(rawValue, propType);
                prop.SetValue(filter, lowerValue);
                return;
            }

            // Limite superior ("até"): se a prop é decorada, o alvo é a vinculada; senão a prop
            // mesma, desde que seja a vinculada de algum par [RangeFilter] do tipo.
            PropertyInfo targetProp;
            if (rangeAttribute != null && !string.IsNullOrWhiteSpace(rangeAttribute.LinkedRangeProperty))
            {
                targetProp = entityType.GetProperty(rangeAttribute.LinkedRangeProperty);
                if (targetProp == null)
                    throw new BwoqCapabilityException(
                        $"Propriedade vinculada '{rangeAttribute.LinkedRangeProperty}' de [RangeFilter] não encontrada em {entityType.Name}.");
            }
            else
            {
                var isLinkedTarget = entityType.GetProperties().Any(candidate =>
                {
                    var linked = candidate.GetCustomAttribute<RangeFilterAttribute>();
                    return linked != null && linked.LinkedRangeProperty == prop.Name;
                });

                if (!isLinkedTarget)
                    throw new BwoqCapabilityException(
                        $"Comparação '<= ' sobre '{prop.Name}' exige decoração [RangeFilter(LinkedRangeProperty = ...)] " +
                        $"ou estar vinculada ao limite superior de algum par para ser expressável pelo GenericRepository.");

                targetProp = prop;
            }

            var upperValue = CoerceValue(rawValue, targetProp.PropertyType);
            targetProp.SetValue(filter, upperValue);
        }

        private static bool IsFilterable(PropertyInfo prop)
        {
            return prop.GetCustomAttribute<FilterableAttribute>() != null;
        }

        private static void EnsureRootOnly(BwoqPredicate predicate, string context)
        {
            if (predicate != null && predicate.HasNavigation)
                throw new BwoqCapabilityException(
                    $"Navegação em '{context}' não é traduzível para o GenericRepository: use a projeção " +
                    "raiz ou os modos SQL (ToSql) / LINQ (Apply).");
        }

        private static object CoerceValue(string rawValue, Type propertyType)
        {
            var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (type == typeof(bool))
                return bool.TryParse(rawValue, out var boolValue)
                    ? (object)boolValue
                    : decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) && number != 0;

            if (type == typeof(DateTime))
                return DateTime.TryParse(rawValue, out var dateValue) ? (object)dateValue : DateTime.MinValue;

            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.TryParse(rawValue, out var offsetValue) ? (object)offsetValue : DateTimeOffset.MinValue;

            if (type == typeof(Guid))
                return Guid.TryParse(rawValue, out var guidValue) ? (object)guidValue : Guid.Empty;

            if (type.IsEnum)
            {
                if (Enum.TryParse(type, rawValue, ignoreCase: true, out var enumValue))
                    return enumValue;
                throw new BwoqCapabilityException($"Valor '{rawValue}' não é um membro válido do enum {type.Name}.");
            }

            if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                return Convert.ChangeType(decimalValue, type, CultureInfo.InvariantCulture);

            return rawValue;
        }

        private static bool IsEmptyDefault(object value)
        {
            if (value == null) return true;
            switch (value)
            {
                case decimal decimalValue: return decimalValue == 0m;
                case double doubleValue: return doubleValue == 0d;
                case float floatValue: return floatValue == 0f;
                case int intValue: return intValue == 0;
                case long longValue: return longValue == 0L;
                case short shortValue: return shortValue == 0;
                case byte byteValue: return byteValue == 0;
                case Guid guid: return guid == Guid.Empty;
                case DateTime date: return date == DateTime.MinValue;
                case DateTimeOffset offset: return offset == DateTimeOffset.MinValue;
                case string str: return string.IsNullOrEmpty(str);
                default:
                    return Convert.ToDecimal(value, CultureInfo.InvariantCulture) == 0m;
            }
        }
    }
}