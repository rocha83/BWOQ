using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Rochas.BWOQ.Helpers
{
    /// <summary>
    /// Tabela bit-wise canónica usada pela lib BWOQ (e consumível por outros assemblies)
    /// para traduzir propriedades em máscaras numéricas.
    ///
    /// Conceito de ORDEM: a hierarquia de herança é lida BASE → DERIVADA.
    /// Campos declarados nas classes-base (ex: Name, IsActive, Id em BaseEntity)
    /// permanecem com os menores índices → menores valores de máscara, já que são
    /// os mais consultados. Propriedades da própria entidade seguem em ordem de
    /// declaração após os da base.
    ///
    /// Exclusões: coleções (ICollection`1) e navegações por classe do mesmo assembly
    /// (entity references) não fazem parte da tabela binária.
    ///
    /// Máscara: BigInteger para não haver teto de 31 propriedades (limite do int).
    /// </summary>
    public static class BitWiseTable
    {
        /// <summary>Máscara (potência de 2) de cada propriedade elegível, base → derivada, na ordem determinada.</summary>
        public static IReadOnlyList<KeyValuePair<PropertyInfo, BigInteger>> GetTable(Type type)
        {
            var props = GetOrderedProps(type);
            var result = new List<KeyValuePair<PropertyInfo, BigInteger>>(props.Length);

            for (var i = 0; i < props.Length; i++)
                result.Add(new KeyValuePair<PropertyInfo, BigInteger>(props[i], BigInteger.One << i));

            return result;
        }

        /// <summary>Propriedades elegíveis na ordem base → derivada.</summary>
        public static PropertyInfo[] GetOrderedProps(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(prp => !prp.PropertyType.Name.Equals("ICollection`1"))
                            .Where(prp => !BuildExclusions(prp, type))
                            .ToArray();

            var order = BuildHierarchyOrder(type);

            return props
                .OrderBy(prp => order.TryGetValue(prp.DeclaringType ?? type, out var lvl) ? lvl : int.MaxValue)
                .ThenBy(prp => Array.IndexOf(props, prp))
                .ToArray();
        }

        /// <summary>Índice (0-based) de uma propriedade na tabela, ou -1 se não elegível.</summary>
        public static int IndexOf(Type type, string propertyName)
        {
            var props = GetOrderedProps(type);
            for (var i = 0; i < props.Length; i++)
                if (props[i].Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>Máscara (BigInteger) de uma propriedade, ou BigInteger.Zero se não elegível.</summary>
        public static BigInteger GetMask(Type type, string propertyName)
        {
            var index = IndexOf(type, propertyName);
            return index < 0 ? BigInteger.Zero : BigInteger.One << index;
        }

        private static bool BuildExclusions(PropertyInfo prp, Type rootType)
        {
            return prp.GetIndexParameters().Any()
                   || (prp.PropertyType.IsClass
                       && prp.PropertyType != typeof(string)
                       && prp.PropertyType.Module.Name.Equals(rootType.Module.Name));
        }

        private static Dictionary<Type, int> BuildHierarchyOrder(Type type)
        {
            var result = new Dictionary<Type, int>();
            var chain = new List<Type>();

            for (var t = type; t != null; t = t.BaseType)
                chain.Add(t);

            var level = 0;
            for (var i = chain.Count - 1; i >= 0; i--)
                result[chain[i]] = level++;

            return result;
        }
    }
}