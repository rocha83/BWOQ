using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using Rochas.BWOQ.Helpers;

namespace Rochas.BWOQ.Data
{
    /// <summary>
    /// Operadores pósfixados de critério na sintaxe BWOQ.
    /// O operador Default é resolvido por tipo da coluna-alvo
    /// (string → semelhança 'like'; demais → igualdade).
    /// </summary>
    public enum BwoqOperator
    {
        Default,
        Equal,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>Pareamento de navegação >ordinal:máscara em composição.</summary>
    public sealed class BwoqNavigation
    {
        public int Ordinal { get; set; }
        public BigInteger Mask { get; set; }

        public BwoqNavigation(int ordinal, BigInteger mask)
        {
            Ordinal = ordinal;
            Mask = mask;
        }
    }

    /// <summary>Fragmento de predicado: máscara raiz + navegações + sufixo de agregação opcional.</summary>
    public sealed class BwoqPredicate
    {
        public BigInteger RootMask { get; set; }
        public List<BwoqNavigation> Navigations { get; } = new List<BwoqNavigation>();
        public char? AggregationSuffix { get; set; }

        public bool HasNavigation => Navigations.Count > 0;
    }

    /// <summary>Fragmento de critério: predicado + valor + combinação AND/OR + operador.</summary>
    public sealed class BwoqCriteria
    {
        public BwoqPredicate Predicate { get; }
        public string RawValue { get; set; }
        public bool IsAnd { get; set; }
        public BwoqOperator Operator { get; set; }

        public BwoqCriteria(BwoqPredicate predicate)
        {
            Predicate = predicate;
        }
    }

    /// <summary>
    /// Parser da gramática BWOQ (sem estados estáticos), reutilizado pelos
    /// tradutores LINQ (motor BitWiseQuery), SQL ANSI e GenericRepository.
    ///
    /// O token '>' é única e exclusivamente navegação em composição
    /// (subclasse/entidade agregada). Nunca representa comparação relacional.
    /// </summary>
    public static class BwoqExpression
    {
        private static readonly Regex CriteriaPattern =
            new Regex(@"^(?<pred>\d+(?:\>\d+:\d+)*)::(?<value>.*?)(?<amp>&?)(?<suffix>(?:=\-|=\+|=|\+|\-)?)$",
                RegexOptions.Compiled);

        private static readonly Regex PredicatePattern =
            new Regex(@"^(?<root>\d+)(?:(?:\>(?<ord>\d+):(?<nmask>\d+))*)(?<suffix>[+\-*~^])?$",
                RegexOptions.Compiled);

        private static readonly Regex SuffixPattern = new Regex(@"[+\-*~^]$", RegexOptions.Compiled);

        /// <summary>Extrai a máscara de um fragmento, ignorando o sufixo de agregação quando presente.</summary>
        public static BigInteger GetMask(string fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment))
                return BigInteger.Zero;

            var clean = SuffixPattern.Replace(fragment, string.Empty);
            if (BigInteger.TryParse(clean, out var mask))
                return mask;
            throw new InvalidQueryExpression();
        }

        /// <summary>Interpreta um fragmento de predicado (Q / O / OD / G).</summary>
        public static BwoqPredicate ParsePredicate(string expression)
        {
            var result = new BwoqPredicate();
            if (string.IsNullOrWhiteSpace(expression))
                return result;

            var match = PredicatePattern.Match(expression);
            if (!match.Success)
                throw new InvalidGroupExpression(); // mesma semântica de fragmento inválido

            if (!BigInteger.TryParse(match.Groups["root"].Value, out var rootMask))
                throw new InvalidQueryExpression();
            result.RootMask = rootMask;

            var ordinals = match.Groups["ord"].Captures;
            var masks = match.Groups["nmask"].Captures;
            for (var i = 0; i < ordinals.Count; i++)
            {
                var ordinal = int.Parse(ordinals[i].Value);
                var mask = BigInteger.Parse(masks[i].Value);
                result.Navigations.Add(new BwoqNavigation(ordinal, mask));
            }

            var suffix = match.Groups["suffix"].Value;
            if (!string.IsNullOrEmpty(suffix))
                result.AggregationSuffix = suffix[0];

            return result;
        }

        /// <summary>Interpreta um fragmento de critério (W) na forma predicado::valor[&][operador].</summary>
        public static BwoqCriteria ParseCriteria(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new InvalidCriteriaExpression();

            var match = CriteriaPattern.Match(expression);
            if (!match.Success)
                throw new InvalidCriteriaExpression();

            var predicate = ParsePredicate(match.Groups["pred"].Value);
            var criteria = new BwoqCriteria(predicate)
            {
                RawValue = match.Groups["value"].Value,
                IsAnd = match.Groups["amp"].Length > 0,
                Operator = ResolveOperator(match.Groups["suffix"].Value)
            };

            return criteria;
        }

        private static BwoqOperator ResolveOperator(string suffix)
        {
            if (suffix.Length == 0)
                return BwoqOperator.Default;
            switch (suffix)
            {
                case "=": return BwoqOperator.Equal;
                case "+": return BwoqOperator.GreaterThan;
                case "-": return BwoqOperator.LessThan;
                case "=+": return BwoqOperator.GreaterOrEqual;
                case "=-": return BwoqOperator.LessOrEqual;
                default: return BwoqOperator.Default;
            }
        }

        #region Resolução de máscaras → propriedades (tabela binária da lib)

        /// <summary>Propriedades do tipo raiz elegíveis para a tabela binária (ordem base → derivada).</summary>
        public static PropertyInfo[] GetRootProperties(Type type)
        {
            return BitWiseTable.GetOrderedProps(type);
        }

        /// <summary>
        /// Propriedades de composição (agregados/subclasses) do tipo raiz, na ordem de declaração.
        /// São os alvos exclusivos do token de navegação '>'.
        /// </summary>
        public static PropertyInfo[] GetCompositionProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .Where(prp => IsCompositionProperty(prp, type))
                       .ToArray();
        }

        /// <summary>Identifica se uma propriedade é de composição (classe do mesmo módulo, exceto string).</summary>
        public static bool IsCompositionProperty(PropertyInfo property, Type rootType)
        {
            return property.PropertyType.IsClass
                   && property.PropertyType != typeof(string)
                   && property.PropertyType.Module.Name.Equals(rootType.Module.Name);
        }

        /// <summary>Resolve as propriedades-raiz eleitas pela máscara (sem navegação).</summary>
        public static PropertyInfo[] ResolveRootProps(Type type, BigInteger mask)
        {
            var props = GetRootProperties(type);
            var unique = new HashSet<int>();
            for (var i = 0; i < props.Length; i++)
                if ((BigInteger.One << i & mask) != 0)
                    unique.Add(i);

            return unique.OrderBy(i => i).Select(i => props[i]).ToArray();
        }

        /// <summary>Resolve o tipo/instância-alvo de uma navegação (ordinal → propriedade de composição).</summary>
        public static PropertyInfo ResolveNavigation(Type rootType, int ordinal)
        {
            var compositionProps = GetCompositionProperties(rootType);
            if (ordinal < 1 || ordinal > compositionProps.Length)
                throw new BwoqCapabilityException(
                    $"Navegação '>{ordinal}' não existe: {rootType.Name} possui {compositionProps.Length} " +
                    $"propriedades de composição. O token '>' é exclusivo para navegação em composição.");

            return compositionProps[ordinal - 1];
        }

        /// <summary>
        /// Projeção LINQ dinâmica de um predicado: raiz + navegações (Aggregate.Prop),
        /// na forma aceita por System.Linq.Dynamic.Core (new(Id, Name, Credential.Logon)).
        /// </summary>
        public static string BuildProjectionExpression(Type entityType, BwoqPredicate predicate)
        {
            var parts = new List<string>();
            parts.AddRange(ResolveRootProps(entityType, predicate.RootMask).Select(p => p.Name));

            foreach (var navigation in predicate.Navigations)
            {
                var composition = ResolveNavigation(entityType, navigation.Ordinal);
                parts.AddRange(ResolveRootProps(composition.PropertyType, navigation.Mask)
                               .Select(p => string.Concat(composition.Name, ".", p.Name)));
            }

            return string.Concat("new (", string.Join(", ", parts), ")");
        }

        #endregion
    }
}