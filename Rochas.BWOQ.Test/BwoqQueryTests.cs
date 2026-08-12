using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Rochas.BWOQ;
using Rochas.BWOQ.Data;
using Rochas.DapperRepository.Specification.Annotations;
using Rochas.DapperRepository.Specification.Enums;

namespace Rochas.BWOQ.Test
{
    public class SqlPerson
    {
        [Column("person_id")]
        public decimal Id { get; set; }

        [Column("nm")]
        public string Name { get; set; } = "";
    }

    [Table("people", Schema = "app")]
    public class SqlPeople
    {
        [Column("person_id")]
        public decimal Id { get; set; }

        [Column("nm")]
        public string Name { get; set; } = "";
    }

    public class RepoClient
    {
        public decimal Id { get; set; }

        [Filterable]
        public string Name { get; set; } = "";

        public decimal Age { get; set; }
        public bool Active { get; set; }

        [RangeFilter(LinkedRangeProperty = "CreditMax")]
        public decimal CreditMin { get; set; }

        public decimal CreditMax { get; set; }
        public string City { get; set; } = "";
    }

    public class BwoqQueryTests
    {
        private readonly List<Person> _testData;

        public BwoqQueryTests()
        {
            _testData = new List<Person>
            {
                new Person { Id = 1, Name = "Carlos Silva", City = "Sao Paulo", State = "SP", Age = 35, Active = true,  CreditLimit = 5000 },
                new Person { Id = 2, Name = "Ana Oliveira", City = "Rio de Janeiro", State = "RJ", Age = 28, Active = true,  CreditLimit = 3000 },
                new Person { Id = 3, Name = "Pedro Santos", City = "Sao Paulo", State = "SP", Age = 42, Active = false, CreditLimit = 8000 },
                new Person { Id = 4, Name = "Maria Costa", City = "Belo Horizonte", State = "MG", Age = 31, Active = true,  CreditLimit = 4500 },
                new Person { Id = 5, Name = "Joao Lima", City = "Curitiba", State = "PR", Age = 55, Active = true,  CreditLimit = 12000 },
                new Person { Id = 6, Name = "Lucia Ferreira", City = "Sao Paulo", State = "SP", Age = 22, Active = false, CreditLimit = 2000 },
                new Person { Id = 7, Name = "Roberto Almeida", City = "Porto Alegre", State = "RS", Age = 38, Active = true,  CreditLimit = 6500 },
                new Person { Id = 8, Name = "Fernanda Ribeiro", City = "Curitiba", State = "PR", Age = 29, Active = true,  CreditLimit = 3800 },
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

        #region Modo 1 — LINQ (Apply)

        [Fact]
        public void Apply_WithoutSelect_ReturnsFullEntities()
        {
            var result = BwoqQuery<Person>.Create()
                .Apply(_testData.AsQueryable())
                .Cast<Person>()
                .ToList();

            Assert.Equal(9, result.Count);
            Assert.All(result, p => Assert.True(p.Id > 0));
        }

        [Fact]
        public void Apply_Select_ProjectsOnlyChosenColumns()
        {
            var result = ((IEnumerable)BwoqQuery<Person>.Create()
                .Select("6") // Name (2) + City (4)
                .Apply(_testData.AsQueryable()))
                .Cast<object>()
                .ToList();

            Assert.Equal(9, result.Count);
            var firstType = result.First().GetType();
            Assert.NotNull(firstType.GetProperty("Name"));
            Assert.NotNull(firstType.GetProperty("City"));
            Assert.Null(firstType.GetProperty("Age"));
        }

        [Fact]
        public void Apply_Where_Equals_FiltersActive()
        {
            var result = BwoqQuery<Person>.Create()
                .Where("32::1&=")
                .Apply(_testData.AsQueryable())
                .Cast<Person>()
                .ToList();

            Assert.Equal(6, result.Count);
            Assert.All(result, p => Assert.True(p.Active));
        }

        [Fact]
        public void Apply_Where_Like_FiltersByName()
        {
            var result = BwoqQuery<Person>.Create()
                .Where("2::carlos")
                .Apply(_testData.AsQueryable())
                .Cast<Person>()
                .ToList();

            Assert.Single(result);
            Assert.All(result, p => Assert.Contains("carlos", p.Name.ToLower()));
        }

        [Fact]
        public void Apply_OrderByDescending_SortsByNameDesc()
        {
            var result = BwoqQuery<Person>.Create()
                .Where("32::1&=")
                .OrderByDescending("2")
                .Apply(_testData.AsQueryable())
                .Cast<Person>()
                .ToList();

            Assert.Equal("Roberto Almeida", result.First().Name);
        }

        [Fact]
        public void Apply_GroupBy_ReturnsDistinctGroups()
        {
            var groups = ((IEnumerable)BwoqQuery<Person>.Create()
                .Where("32::1&=")
                .GroupBy("4", "4")
                .Apply(_testData.AsQueryable()))
                .Cast<object>()
                .ToList();

            Assert.Equal(5, groups.Count);
        }

        [Fact]
        public void Apply_Navigation_ProjectsAggregateColumns()
        {
            var result = ((IEnumerable)BwoqQuery<Employee>.Create()
                .Select("3>1:2")
                .Apply(_employeeData().AsQueryable()))
                .Cast<object>()
                .ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result.First().GetType().GetProperties(), prp => prp.Name.Contains("Logon"));
        }

        #endregion

        #region Modo 2 — SQL ANSI (ToSql)

        [Fact]
        public void ToSql_NoSelect_ReturnsAllColumns()
        {
            var sql = BwoqQuery<Person>.Create().ToSql(DatabaseEngine.SQLServer);

            Assert.Equal("SELECT * FROM Person", sql);
        }

        [Fact]
        public void ToSql_Select_ProjectsChosenColumns()
        {
            var sql = BwoqQuery<Person>.Create()
                .Select("6") // Name (2) + City (4)
                .ToSql(DatabaseEngine.SQLServer);

            Assert.Equal("SELECT Name, City FROM Person", sql);
        }

        [Fact]
        public void ToSql_WhereEquals_AddsClause()
        {
            var sql = BwoqQuery<Person>.Create()
                .Where("32::1&=")
                .ToSql(DatabaseEngine.SQLServer);

            Assert.Equal("SELECT * FROM Person WHERE (Active = 1)", sql);
        }

        [Fact]
        public void ToSql_WhereLike_UsesLower()
        {
            var sql = BwoqQuery<Person>.Create()
                .Where("2::carlos")
                .ToSql(DatabaseEngine.SQLServer);

            Assert.Equal("SELECT * FROM Person WHERE (LOWER(Name) LIKE LOWER('%carlos%'))", sql);
        }

        [Fact]
        public void ToSql_WhereGreaterOrEqual_UsesOperator()
        {
            var sql = BwoqQuery<Person>.Create()
                .Where("16::35=+")
                .ToSql(DatabaseEngine.SQLServer);

            Assert.Equal("SELECT * FROM Person WHERE (Age >= 35)", sql);
        }

        [Fact]
        public void ToSql_OrderBy_AppendsAscendingByDefault()
        {
            var sql = BwoqQuery<Person>.Create()
                .OrderBy("2")
                .ToSql(DatabaseEngine.MySQL);

            Assert.Equal("SELECT * FROM Person ORDER BY Name ASC", sql);
        }

        [Fact]
        public void ToSql_OrderByDescending_AppendsDescending()
        {
            var sql = BwoqQuery<Person>.Create()
                .OrderByDescending("2")
                .ToSql(DatabaseEngine.MySQL);

            Assert.Equal("SELECT * FROM Person ORDER BY Name DESC", sql);
        }

        [Fact]
        public void ToSql_GroupByWithSum_AddsAggregationClause()
        {
            var sql = BwoqQuery<Person>.Create()
                .GroupBy("64^", "4") // CreditLimit (64) suma, agrupado por City (4)
                .ToSql(DatabaseEngine.SQLite);

            Assert.Equal("SELECT City, SUM(CreditLimit) AS SumOfCreditLimits FROM Person GROUP BY City", sql);
        }

        [Fact]
        public void ToSql_Postgres_QuotesIdentifiers()
        {
            var sql = BwoqQuery<Person>.Create()
                .Select("6")
                .ToSql(DatabaseEngine.PostgreSQL);

            Assert.Equal("SELECT \"Name\", \"City\" FROM \"Person\"", sql);
        }

        [Fact]
        public void ToSql_ColumnAndSchemaAttributes_Resolved()
        {
            var sql = BwoqQuery<SqlPeople>.Create()
                .Select("1") // Id (1) → person_id
                .ToSql(DatabaseEngine.MySQL);

            Assert.Equal("SELECT person_id FROM app.people", sql);
        }

        [Fact]
        public void ToSql_NavigationOnSelect_ThrowsCapability()
        {
            var query = BwoqQuery<Person>.Create().Select("3>1:2");

            var ex = Assert.Throws<BwoqCapabilityException>(() => query.ToSql(DatabaseEngine.MySQL));
            Assert.Contains("não pode ser traduzida", ex.Message);
        }

        [Fact]
        public void ToSql_CriteriaOnNavigation_ThrowsCapability()
        {
            var query = BwoqQuery<Person>.Create().Where("2>1:2::carlos");

            Assert.Throws<BwoqCapabilityException>(() => query.ToSql(DatabaseEngine.MySQL));
        }

        [Fact]
        public void ToSql_GroupWithoutBy_ThrowsInvalidGroup()
        {
            var query = BwoqQuery<Person>.Create().GroupBy("64^", "");

            Assert.Throws<InvalidGroupExpression>(() => query.ToSql(DatabaseEngine.MySQL));
        }

        #endregion

        #region Modo 3 — GenericRepository (ToRepositoryQuery)

        [Fact]
        public void ToRepositoryQuery_Equals_ReflectsIntoFilter()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .Where("8::1&=") // Active
                .Where("1::7&=") // Id
                .ToRepositoryQuery();

            Assert.NotNull(target.Filter);
            Assert.True(target.Filter.Active);
            Assert.Equal(7m, target.Filter.Id);
            Assert.False(target.UseSearch);
            Assert.False(target.LoadComposition);
        }

        [Fact]
        public void ToRepositoryQuery_Search_ActivatesSearchPath()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .Where("2::carlos") // Name [Filterable]
                .ToRepositoryQuery();

            Assert.True(target.UseSearch);
            Assert.Equal("carlos", target.SearchCriteria);
            Assert.False(target.LoadComposition);
        }

        [Fact]
        public void ToRepositoryQuery_OrderBy_ExportsAttributes()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .OrderBy("64") // City
                .ToRepositoryQuery();

            Assert.Equal(new[] { "City" }, target.OrderByAttributes);
            Assert.False(target.OrderDescending);
        }

        [Fact]
        public void ToRepositoryQuery_OrderByDescending_ExportsDirection()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .OrderByDescending("64")
                .ToRepositoryQuery();

            Assert.Equal(new[] { "City" }, target.OrderByAttributes);
            Assert.True(target.OrderDescending);
        }

        [Fact]
        public void ToRepositoryQuery_GroupBy_ExportsGroupAndAggregation()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .GroupBy("2^", "64") // Name agregado (SUM), agrupado por City
                .ToRepositoryQuery();

            Assert.Equal(new[] { "City" }, target.GroupByAttributes);
            Assert.Equal(DataAggregationType.Sum, target.Aggregates["Name"]);
        }

        [Fact]
        public void ToRepositoryQuery_LoadComposition_EnabledOnNavigationSelect()
        {
            var target = BwoqQuery<Employee>.Create()
                .Select("3>1:2")
                .ToRepositoryQuery();

            Assert.True(target.LoadComposition);
            Assert.NotNull(target.Filter);
        }

        [Fact]
        public void ToRepositoryQuery_Range_ReflectsBounds()
        {
            var target = BwoqQuery<RepoClient>.Create()
                .Where("16::100&=+") // CreditMin >= 100
                .Where("32::500&=-") // CreditMax <= 500
                .ToRepositoryQuery();

            Assert.Equal(100m, target.Filter.CreditMin);
            Assert.Equal(500m, target.Filter.CreditMax);
        }

        [Fact]
        public void ToRepositoryQuery_StrictComparison_ThrowsCapability()
        {
            var query = BwoqQuery<RepoClient>.Create().Where("4::35+"); // Age estrito (>)

            var ex = Assert.Throws<BwoqCapabilityException>(() => query.ToRepositoryQuery());
            Assert.Contains("não é expressável", ex.Message);
        }

        [Fact]
        public void ToRepositoryQuery_BooleanFalse_ThrowsCapability()
        {
            var query = BwoqQuery<RepoClient>.Create().Where("8::0="); // Active = false

            Assert.Throws<BwoqCapabilityException>(() => query.ToRepositoryQuery());
        }

        [Fact]
        public void ToRepositoryQuery_NavigationOnCriteria_ThrowsCapability()
        {
            var query = BwoqQuery<Employee>.Create().Where("2>1:2::ana");

            Assert.Throws<BwoqCapabilityException>(() => query.ToRepositoryQuery());
        }

        [Fact]
        public void ToRepositoryQuery_MultipleCriteriaWithOr_ThrowsCapability()
        {
            var query = BwoqQuery<RepoClient>.Create()
                .Where("2::carlos")
                .Where("8::1="); // segundo critério sem '&' → disjunção

            Assert.Throws<BwoqCapabilityException>(() => query.ToRepositoryQuery());
        }

        [Fact]
        public void ToRepositoryQuery_NullEquality_ThrowsCapability()
        {
            var query = BwoqQuery<RepoClient>.Create().Where("1::null=");

            Assert.Throws<BwoqCapabilityException>(() => query.ToRepositoryQuery());
        }

        #endregion
    }
}