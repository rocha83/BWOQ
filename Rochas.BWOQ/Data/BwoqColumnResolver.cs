using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Rochas.DapperRepository.Specification.Enums;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Metadados de coluna/tabela (via DataAnnotations [Table]/[Column] ou
    /// convenção PascalCase), formatação de literais e identificadores por dialeto.
    /// Replica a inteligência madura de tradução SQL da ORM Dapper usando apenas
    /// a porção pública do contrato (System.ComponentModel.DataAnnotations).
    /// </summary>
    public static class BwoqColumnResolver
    {
        #region Tabela / Coluna

        public static string GetTableName(Type type)
        {
            var attr = type.GetCustomAttribute<TableAttribute>();
            if (attr == null)
                return type.Name;

            return string.IsNullOrWhiteSpace(attr.Schema)
                ? attr.Name
                : string.Concat(attr.Schema, ".", attr.Name);
        }

        public static string GetColumnName(PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<ColumnAttribute>();
            return attr?.Name ?? property.Name;
        }

        #endregion

        #region Dialeto

        /// <summary>Delimita um identificador conforme o motor. PostgreSql usa aspas por parte.</summary>
        public static string QuoteIdentifier(string name, DatabaseEngine engine)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            if (engine != DatabaseEngine.PostgreSQL)
                return name;

            return string.Join(".", name.Split('.').Select(part => string.Concat("\"", part, "\"")));
        }

        /// <summary>Formata um valor como literal SQL para o dialeto informado.</summary>
        public static string FormatLiteral(object value, DatabaseEngine engine)
        {
            if (value == null)
                return "NULL";

            switch (value)
            {
                case bool boolValue:
                    return engine == DatabaseEngine.PostgreSQL
                        ? (boolValue ? "TRUE" : "FALSE")
                        : (boolValue ? "1" : "0");
                case DateTime dateValue:
                    return string.Concat("'", dateValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "'");
                case DateTimeOffset offsetValue:
                    return string.Concat("'", offsetValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "'");
                case string stringValue:
                    return string.Concat("'", stringValue.Replace("'", "''"), "'");
                default:
                    if (IsNumericLike(value))
                        return Convert.ToString(value, CultureInfo.InvariantCulture);
                    return string.Concat("'", Convert.ToString(value, CultureInfo.InvariantCulture).Replace("'", "''"), "'");
            }
        }

        private static bool IsNumericLike(object value)
        {
            var typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return value is Enum;
            }
        }

        #endregion

        #region Comparações

        /// <summary>
        /// Comparação por tipo: sem operador explícito, string → semelhança (LIKE),
        /// demais → igualdade. Operadores explícitos viram símbolos ANSI.
        /// </summary>
        public static string BuildComparison(string column, Type propertyType, string rawValue,
                                             BwoqOperator op, DatabaseEngine engine)
        {
            if (rawValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                return string.Concat(column, " IS NULL");

            var typedValue = ConvertValue(rawValue, propertyType);
            var literal = FormatLiteral(typedValue, engine);
            var effectiveOp = EffectiveOperator(op, propertyType, rawValue);

            switch (effectiveOp)
            {
                case BwoqOperator.Equal: return string.Concat(column, " = ", literal);
                case BwoqOperator.GreaterThan: return string.Concat(column, " > ", literal);
                case BwoqOperator.LessThan: return string.Concat(column, " < ", literal);
                case BwoqOperator.GreaterOrEqual: return string.Concat(column, " >= ", literal);
                case BwoqOperator.LessOrEqual: return string.Concat(column, " <= ", literal);
                default:
                    var likeValue = string.Concat("%", rawValue.Replace("'", "''"), "%");
                    return string.Concat("LOWER(", column, ") LIKE LOWER('%", likeValue, "')");
            }
        }

        private static BwoqOperator EffectiveOperator(BwoqOperator op, Type propertyType, string rawValue)
        {
            if (op != BwoqOperator.Default)
                return op;

            if (IsString(propertyType) && decimal.TryParse(rawValue, out _) == false
                    && bool.TryParse(rawValue, out _) == false
                    && DateTime.TryParse(rawValue, out _) == false)
                return BwoqOperator.Default;

            return BwoqOperator.Equal;
        }

        internal static bool IsString(Type propertyType)
        {
            return propertyType == typeof(string);
        }

        /// <summary>Converte o valor do critério para o tipo da propriedade (semântica de setObjValCombin).</summary>
        public static object ConvertValue(string rawValue, Type propertyType)
        {
            var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (rawValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (type == typeof(bool))
            {
                return bool.TryParse(rawValue, out var boolValue)
                    ? (object)boolValue
                    : decimal.TryParse(rawValue, out var numValue) && numValue != 0;
            }

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(rawValue, out var dateValue))
                    return dateValue;
                return DateTime.MinValue;
            }

            if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                return decimalValue;

            return rawValue;
        }

        #endregion

        #region Agregações

        /// <summary>Função SQL da agregação e alias gerado (convenção da lib).</summary>
        public static (string Function, string Alias) BuildAggregation(char suffix, string column, string propertyName)
        {
            switch (suffix)
            {
                case '*': return ("COUNT", "CountResult");
                case '^': return ("SUM", string.Concat("SumOf", propertyName, "s"));
                case '~': return ("AVG", string.Concat("AverageOf", propertyName, "s"));
                case '+': return ("MAX", string.Concat("MaximumOf", propertyName, "s"));
                case '-': return ("MIN", string.Concat("MinimumOf", propertyName, "s"));
                default: throw new InvalidGroupExpression();
            }
        }

        /// <summary>Mapeia o sufixo para o tipo de agregação da Specification (modo repositório).</summary>
        public static DataAggregationType? ResolveAggregationType(char? suffix)
        {
            switch (suffix)
            {
                case '*': return DataAggregationType.Count;
                case '^': return DataAggregationType.Sum;
                case '~': return DataAggregationType.Average;
                case '+': return DataAggregationType.Maximum;
                case '-': return DataAggregationType.Minimum;
                default: return null;
            }
        }

        #endregion
    }
}