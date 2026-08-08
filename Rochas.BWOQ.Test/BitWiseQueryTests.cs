using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rochas.BWOQ;
using Rochas.BWOQ.Helpers;

namespace Rochas.BWOQ.Test
{
    public class Person
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public decimal Age { get; set; }
        public bool Active { get; set; }
        public decimal CreditLimit { get; set; }
    }

    public class Credential
    {
        public decimal Id { get; set; }
        public string Logon { get; set; } = "";
        public string TokenId { get; set; } = "";
    }

    public class Employee
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Age { get; set; }
        public bool Active { get; set; }
        public Credential Credential { get; set; } = new Credential();
    }

    public class BitWiseQueryTests
    {
        private readonly List<Person> _testData;

        public BitWiseQueryTests()
        {
            _testData = new List<Person>
            {
                new Person { Id = 1, Name = "Carlos Silva", City = "São Paulo", State = "SP", Age = 35, Active = true, CreditLimit = 5000 },
                new Person { Id = 2, Name = "Ana Oliveira", City = "Rio de Janeiro", State = "RJ", Age = 28, Active = true, CreditLimit = 3000 },
                new Person { Id = 3, Name = "Pedro Santos", City = "São Paulo", State = "SP", Age = 42, Active = false, CreditLimit = 8000 },
                new Person { Id = 4, Name = "Maria Costa", City = "Belo Horizonte", State = "MG", Age = 31, Active = true, CreditLimit = 4500 },
                new Person { Id = 5, Name = "João Lima", City = "Curitiba", State = "PR", Age = 55, Active = true, CreditLimit = 12000 },
                new Person { Id = 6, Name = "Lucia Ferreira", City = "São Paulo", State = "SP", Age = 22, Active = false, CreditLimit = 2000 },
                new Person { Id = 7, Name = "Roberto Almeida", City = "Porto Alegre", State = "RS", Age = 38, Active = true, CreditLimit = 6500 },
                new Person { Id = 8, Name = "Fernanda Ribeiro", City = "Curitiba", State = "PR", Age = 29, Active = true, CreditLimit = 3800 },
                new Person { Id = 9, Name = "Marcos Pereira", City = "Rio de Janeiro", State = "RJ", Age = 45, Active = false, CreditLimit = 7200 },
            };
        }

        private List<Employee> _employeeData()
        {
            return new List<Employee>
            {
                new Employee { Id = 1, Name = "Carlos", Age = 35, Active = true, Credential = new Credential { Id = 1, Logon = "carlos.silva", TokenId = "TK-001" } },
                new Employee { Id = 2, Name = "Ana", Age = 28, Active = true, Credential = new Credential { Id = 2, Logon = "ana.oliveira", TokenId = "TK-002" } },
                new Employee { Id = 3, Name = "Pedro", Age = 42, Active = false, Credential = new Credential { Id = 3, Logon = "pedro.santos", TokenId = "TK-003" } },
            };
        }

        #region Q() Tests - Select Columns (Predicate)

        [Fact]
        public void Q_SelectNameAndCity_ReturnsProjectedColumns()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IEnumerable)bwq.Q("6", true)).Cast<object>().ToList();

            Assert.NotNull(result);
            Assert.Equal(9, result.Count);

            var firstItemType = result.First().GetType();
            Assert.NotNull(firstItemType.GetProperty("Name"));
            Assert.NotNull(firstItemType.GetProperty("City"));
            Assert.Null(firstItemType.GetProperty("Age"));
        }

        [Fact]
        public void Q_SelectWithNavigation_ReturnsAggregateProjection()
        {
            var bwq = new BitWiseQuery<Employee>(_employeeData().AsQueryable());
            var result = ((IEnumerable)bwq.Q("3>1:2", true)).Cast<object>().ToList();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            var firstItemType = result.First().GetType();
            Assert.NotNull(firstItemType.GetProperty("Id"));
            Assert.NotNull(firstItemType.GetProperty("Name"));
            Assert.Contains(firstItemType.GetProperties(), prp => prp.Name.Contains("Logon"));
        }

        [Fact]
        public void Q_FilterBuilder_ExposesSourceObjects()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.Equal(9, result.Count);
        }

        #endregion

        #region W() Tests - Filter (Criteria)

        [Fact]
        public void W_EqualityConjunction_ReturnsActivePersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("32::1&=")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Active));
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void W_BooleanLiteral_ReturnsActivePersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("32::true")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Active));
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void W_Like_ReturnsMatchingPersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("2::carlos")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.Contains("carlos", p.Name.ToLower()));
            Assert.Single(result);
        }

        [Fact]
        public void W_GreaterThan_ReturnsOlderPersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("16::40+")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Age > 40));
        }

        [Fact]
        public void W_GreaterOrEqual_ReturnsPersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("16::35=+")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Age >= 35));
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void W_LessOrEqual_ReturnsPersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("16::35=-")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Age <= 35));
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void W_LessThan_ReturnsYoungerPersons()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("16::30-")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.All(result, p => Assert.True(p.Age < 30));
        }

        #endregion

        #region O() / OD() Tests - OrderBy

        [Fact]
        public void O_OrderByAscending_ReturnsSortedByName()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("32::1&=").O("2")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.Equal("Ana Oliveira", result.First().Name);
        }

        [Fact]
        public void OD_OrderByDescending_ReturnsSortedByNameDesc()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = ((IQueryable)bwq.Q("127").W("32::1&=").OD("2")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.Equal("Roberto Almeida", result.First().Name);
        }

        #endregion

        #region G() Tests - GroupBy

        [Fact]
        public void G_GroupByCity_ReturnsDistinctGroups()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var groups = ((IEnumerable)bwq.Q("127").W("32::1&=").G("4", "4")).Cast<object>().ToList();

            Assert.NotNull(groups);
            Assert.Equal(5, groups.Count);
        }

        [Fact]
        public void G_CountAggregation_SumsGroupItems()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var items = ((IEnumerable)bwq.Q("127").W("32::1&=").G("4*", "4")).Cast<object>().ToList();

            Assert.NotNull(items);
            Assert.Equal(5, items.Count);

            var total = items.Sum(it => (int)(it.GetType().GetProperty("CountResult")?.GetValue(it) ?? 0));
            Assert.Equal(6, total);
        }

        [Fact]
        public void G_SumAggregation_MaximumByCity()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var items = ((IEnumerable)bwq.Q("127").W("32::1&=").G("64+", "4")).Cast<object>().ToList();

            Assert.NotNull(items);
            Assert.Equal(5, items.Count);

            var prop = items.First().GetType().GetProperty("MaximumOfCreditLimits");
            Assert.NotNull(prop);

            var maximum = items.Max(it => (decimal)(prop.GetValue(it) ?? 0));
            Assert.Equal(12000m, maximum);
        }

        [Fact]
        public void G_SumAggregation_SumsByCity()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var items = ((IEnumerable)bwq.Q("127").W("32::1&=").G("64^", "4")).Cast<object>().ToList();

            Assert.NotNull(items);
            Assert.Equal(5, items.Count);

            var prop = items.First().GetType().GetProperty("SumOfCreditLimits");
            Assert.NotNull(prop);

            var total = items.Sum(it => (decimal)(prop.GetValue(it) ?? 0));
            Assert.Equal(34800m, total);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public void Q_Navigation_ProjectsAggregateColumns()
        {
            var bwq = new BitWiseQuery<Employee>(_employeeData().AsQueryable());
            var result = ((IEnumerable)bwq.Q("3>1:2", true)).Cast<object>().ToList();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            var firstItemType = result.First().GetType();
            Assert.NotNull(firstItemType.GetProperty("Id"));
            Assert.NotNull(firstItemType.GetProperty("Name"));
            Assert.Contains(firstItemType.GetProperties(), prp => prp.Name.Contains("Logon"));
        }

        [Fact]
        public void W_WithNavigation_FiltersOnAggregateProperty()
        {
            var bwq = new BitWiseQuery<Employee>(_employeeData().AsQueryable());
            var result = ((IQueryable)bwq.Q("15").W("2>1:2::ana")).Cast<Employee>().ToList();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Ana", result[0].Name);
        }

        #endregion

        #region Hierarchy + BigInteger tests

        public abstract class BaseRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public bool IsActive { get; set; } = true;
        }

        public class DerivedRow : BaseRow
        {
            public DateTime CreatedAt { get; set; }
            public decimal CreditLimit { get; set; }
            public string City { get; set; } = "";
            public string State { get; set; } = "";
            public int Age { get; set; }
            public string Document { get; set; } = "";
        }

        // 40 propriedades reais: index > 31 exige BigInteger (int estouraria).
        public class WideRow
        {
            public string C01 { get; set; } = "";
            public string C02 { get; set; } = "";
            public string C03 { get; set; } = "";
            public string C04 { get; set; } = "";
            public string C05 { get; set; } = "";
            public string C06 { get; set; } = "";
            public string C07 { get; set; } = "";
            public string C08 { get; set; } = "";
            public string C09 { get; set; } = "";
            public string C10 { get; set; } = "";
            public string C11 { get; set; } = "";
            public string C12 { get; set; } = "";
            public string C13 { get; set; } = "";
            public string C14 { get; set; } = "";
            public string C15 { get; set; } = "";
            public string C16 { get; set; } = "";
            public string C17 { get; set; } = "";
            public string C18 { get; set; } = "";
            public string C19 { get; set; } = "";
            public string C20 { get; set; } = "";
            public string C21 { get; set; } = "";
            public string C22 { get; set; } = "";
            public string C23 { get; set; } = "";
            public string C24 { get; set; } = "";
            public string C25 { get; set; } = "";
            public string C26 { get; set; } = "";
            public string C27 { get; set; } = "";
            public string C28 { get; set; } = "";
            public string C29 { get; set; } = "";
            public string C30 { get; set; } = "";
            public string C31 { get; set; } = "";
            public string C32 { get; set; } = "";
            public string C33 { get; set; } = "";
            public string C34 { get; set; } = "";
            public string C35 { get; set; } = "";
            public string C36 { get; set; } = "";
            public string C37 { get; set; } = "";
            public string C38 { get; set; } = "";
            public string C39 { get; set; } = "";
            public decimal Value { get; set; }
        }

        [Fact]
        public void Table_BaseProps_GetLowestIndices()
        {
            var table = BitWiseTable.GetOrderedProps(typeof(DerivedRow));

            var nameIdx = Array.FindIndex(table, p => p.Name == "Name");
            var idIdx = Array.FindIndex(table, p => p.Name == "Id");
            var creditIdx = Array.FindIndex(table, p => p.Name == "CreditLimit");
            var stateIdx = Array.FindIndex(table, p => p.Name == "State");

            Assert.True(idIdx < creditIdx, $"Id (idx {idIdx}) must come before CreditLimit (idx {creditIdx})");
            Assert.True(nameIdx < creditIdx, $"Name (idx {nameIdx}) must come before CreditLimit (idx {creditIdx})");
            Assert.True(idIdx < stateIdx, $"Id (idx {idIdx}) must come before State (idx {stateIdx})");
        }

        [Fact]
        public void Table_Masks_AreBigInteger_WithoutIntOverflow()
        {
            var props = BitWiseTable.GetOrderedProps(typeof(WideRow));
            var valueIdx = Array.FindIndex(props, p => p.Name == "Value");

            Assert.Equal(39, valueIdx);
            var mask = BigInteger.One << valueIdx;
            Assert.True(mask > int.MaxValue, "Máscara deve ultrapassar o limite do int sem overflow.");
        }

        [Fact]
        public void Q_Selects_PropertyBeyondBit31()
        {
            var bwq = new BitWiseQuery<WideRow>(_wideData().AsQueryable());
            var mask = GetMaskOf(typeof(WideRow), "Value");
            var result = ((IEnumerable)bwq.Q(mask.ToString(), true)).Cast<object>().ToList();

            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.NotNull(r.GetType().GetProperty("Value")));
        }

        [Fact]
        public void W_Filters_OnPropertyBeyondBit31()
        {
            var bwq = new BitWiseQuery<WideRow>(_wideData().AsQueryable());
            var mask = GetMaskOf(typeof(WideRow), "Value");
            var result = ((IQueryable)bwq.Q("3").W($"{mask}::8500=")).Cast<WideRow>().ToList();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(8500m, result[0].Value);
        }

        [Fact]
        public void W_Filters_OnBaseProperty_UsingLowMask()
        {
            var bwq = new BitWiseQuery<DerivedRow>(_derivedData().AsQueryable());
            var mask = GetMaskOf(typeof(DerivedRow), "IsActive");
            var result = ((IQueryable)bwq.Q("3").W($"{mask}::1&=")).Cast<DerivedRow>().ToList();

            Assert.NotNull(result);
            Assert.All(result, r => Assert.True(r.IsActive));
        }

        private static List<DerivedRow> _derivedData() => new()
        {
            new DerivedRow { Id = 1, Name = "Carlos", IsActive = true,  Age = 35, CreditLimit = 8500, State = "SP" },
            new DerivedRow { Id = 2, Name = "Laura",   IsActive = false, Age = 28, CreditLimit = 3500, State = "RJ" },
            new DerivedRow { Id = 3, Name = "Joao",    IsActive = true,  Age = 42, CreditLimit = 5000, State = "MG" },
        };

        private static List<WideRow> _wideData() => new()
        {
            new WideRow { Value = 8500 },
            new WideRow { Value = 3500 },
            new WideRow { Value = 5000 },
        };

        private static BigInteger GetMaskOf(Type type, string propertyName)
        {
            var props = BitWiseTable.GetOrderedProps(type);
            var index = Array.FindIndex(props, p => p.Name == propertyName);
            Assert.True(index >= 0, $"Propriedade {propertyName} não encontrada.");
            return BigInteger.One << index;
        }

        #endregion
    }
}
