#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Rochas.BWOQ;
using Rochas.BWOQ.Helpers;

namespace Rochas.BWOQ.Test
{
    public class OrderLine
    {
        public decimal Id { get; set; }
        public string Product { get; set; } = "";
        public int Quantity { get; set; }
    }

    public class Order
    {
        public decimal Id { get; set; }
        public string Description { get; set; } = "";
        public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
        public Credential Metadata { get; set; } = new Credential();
    }

    public class AdditionalCoverageTests
    {
        private readonly List<Person> _testData;

        public AdditionalCoverageTests()
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

        #region Constructors + serialization overloads

        [Fact]
        public void Ctor_IList_ClonesInstances()
        {
            var source = new ArrayList { _testData[0], _testData[1] };
            var bwq = new BitWiseQuery<Person>(source, typeof(Person));
            var result = ((IQueryable)bwq.Q("127")).Cast<Person>().ToList();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Carlos Silva", result[0].Name);
        }

        [Fact]
        public void Ctor_RefParameters_EnablesPredicateSelect()
        {
            var q = _testData.AsQueryable();
            var expr = "127";
            var bwq = new BitWiseQuery<Person>(ref q, ref expr, null);
            var result = ((IEnumerable)bwq.Where("32::1&=")).Cast<object>().ToList();

            Assert.NotNull(result);
            Assert.Equal(6, result.Count);
            Assert.NotNull(result.First().GetType().GetProperty("Name"));
        }

        [Fact]
        public void Q_SerializesProjectionToJson()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var json = bwq.Q("6", EnumSerialDataType.JSON);

            Assert.False(string.IsNullOrEmpty(json));
        }

        [Fact]
        public void W_SerializesActiveToCsv()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var csv = bwq.W("32::1&=", EnumSerialDataType.CSV);

            Assert.Contains("Name;City", csv);
            Assert.Contains("Carlos Silva", csv);
        }

        [Fact]
        public void W_SerializesActiveToJson()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var json = bwq.W("32::1&=", EnumSerialDataType.JSON);

            Assert.Contains("\"Name\"", json);
        }

        [Fact]
        public void O_SerializesToCsv()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var csv = bwq.O("2", EnumSerialDataType.CSV);

            Assert.Contains("Name;", csv);
            Assert.Contains("Ana Oliveira", csv);
        }

        [Fact]
        public void OD_SerializesToCsv()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var csv = bwq.OD("2", EnumSerialDataType.CSV);

            Assert.Contains("Name;", csv);
            Assert.Contains("Roberto Almeida", csv);
        }

        [Fact]
        public void G_SerializesAggregateToCsv()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var csv = bwq.G("4*", "4", EnumSerialDataType.CSV);

            Assert.NotNull(csv);
            Assert.Contains("City", csv);
        }

        [Fact]
        public void G_AverageAggregation_ExposesAverageOfAges()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var items = ((IEnumerable)bwq.Q("127").W("32::1&=").G("16~", "4")).Cast<object>().ToList();

            Assert.NotNull(items);
            Assert.Equal(5, items.Count);
            Assert.NotNull(items.First().GetType().GetProperty("AverageOfAges"));
        }

        [Fact]
        public void G_MinAggregation_ExposesMinimumOfCreditLimits()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var items = ((IEnumerable)bwq.Q("127").W("32::1&=").G("64-", "4")).Cast<object>().ToList();

            Assert.NotNull(items);
            Assert.Equal(5, items.Count);
            Assert.NotNull(items.First().GetType().GetProperty("MinimumOfCreditLimits"));
        }

        #endregion

        #region Invalid expressions / error branches

        [Fact]
        public void Query_InvalidPredicate_ThrowsInvalidQueryExpression()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            Assert.Throws<InvalidQueryExpression>(() => bwq.Q("abc", true));
        }

        [Fact]
        public void Where_InvalidCriteria_ThrowsInvalidCriteriaExpression()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            Assert.Throws<InvalidCriteriaExpression>(() => bwq.W("xyz"));
        }

        [Fact]
        public void Where_NumericWithoutComparator_ThrowsInvalidCriteriaAttribute()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            Assert.Throws<InvalidCriteriaAttribute>(() => bwq.W("16::40"));
        }

        [Fact]
        public void Where_StringCriteriaWithNumComparator_TakesInvalidCombinatorBranch()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = bwq.W("2::carlos+");

            Assert.Null(result);
        }

        [Fact]
        public void Where_InternalCombn_AndOperator_IsBuilt()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = bwq.W("6::10<&=");

            Assert.NotNull(result);
        }

        [Fact]
        public void Where_InternalCombn_OrOperator_IsBuilt()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var result = bwq.W("6::10<=");

            Assert.NotNull(result);
        }

        [Fact]
        public void Where_InternalCombn_CompareTokenVariants_AreBuilt()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());

            Assert.NotNull(bwq.W("6::10<=+"));
            Assert.NotNull(bwq.W("6::10<=-"));
            Assert.NotNull(bwq.W("6::10<+"));
            Assert.NotNull(bwq.W("6::10<-"));
        }

        [Fact]
        public void Where_InternalCombn_OddProps_ThrowsInvalidInternalCombin()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            Assert.Throws<InvalidInternalCombinAttribute>(() => bwq.W("2::10<="));
        }

        [Fact]
        public void All_CustomExceptions_CarryDescriptions()
        {
            Assert.Contains("Invalid query", new InvalidQueryExpression().Message);
            Assert.Contains("Invalid criteria expression", new InvalidCriteriaExpression().Message);
            Assert.Contains("Invalid group", new InvalidGroupExpression().Message);
            Assert.Contains("comparation attributes", new InvalidInternalCombinAttribute().Message);
            Assert.Contains("String criteria", new InvalidCriterCombinAttribute().Message);
            Assert.Contains("Numeric, DateTime", new InvalidCriteriaAttribute().Message);
        }

        [Fact]
        public void VwsAliases_DelegateToLongMethods()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());

            var filtered = ((IEnumerable)bwq.W("32::1&=")).Cast<Person>().ToList();
            Assert.Equal(6, filtered.Count);

            var ordered = ((IEnumerable)bwq.O("2")).Cast<Person>().ToList();
            Assert.Equal("Ana Oliveira", ordered.First().Name);

            var desc = ((IEnumerable)bwq.OD("2")).Cast<Person>().ToList();
            Assert.Equal("Roberto Almeida", desc.First().Name);

            var grouped = ((IEnumerable)bwq.G("4", "4")).Cast<object>().ToList();
            Assert.Equal(5, grouped.Count);
        }

        [Fact]
        public void Query_PrivateHelper_GetDynExprPredic_JoinsProps()
        {
            var bwq = new BitWiseQuery<Person>(_testData.AsQueryable());
            var method = typeof(BitWiseQuery<Person>).GetMethod("getDynExprPredic",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var result = (string)method.Invoke(bwq, new object[] { new[] { "Name", "City" } });

            Assert.Equal("Name, City", result);
        }

        #endregion

        #region BWQFilter coverage

        [Fact]
        public void BWQFilter_WhereWithoutPredicate_ReturnsTypedResults()
        {
            var filter = new BWQFilter<Person>(_testData.AsQueryable(), "");
            var result = filter.Where("32::1&=");

            Assert.NotNull(result);
            Assert.Equal(6, result.Count());
        }

        [Fact]
        public void BWQFilter_ExposesElementType()
        {
            var filter = new BWQFilter<Person>(_testData.AsQueryable(), "");

            Assert.Equal(typeof(Person), filter.ElementType);
        }

        [Fact]
        public void BWQFilter_EnumeratesGenericAndNonGeneric()
        {
            var filter = new BWQFilter<Person>(_testData.AsQueryable(), "");

            var typed = ((IEnumerable<Person>)filter).ToArray();
            Assert.Equal(9, typed.Length);

            var untyped = ((IEnumerable)filter).GetEnumerator();
            Assert.NotNull(untyped);
            ((IDisposable)untyped).Dispose();
        }

        [Fact]
        public void BWQFilter_GroupByNonBuilder_ReturnsNullCastResult()
        {
            var filter = new BWQFilter<Person>(_testData.AsQueryable(), "");
            var result = filter.GroupBy("4", "4");

            Assert.Null(result);
        }

        #endregion

        #region Reflector coverage

        [Fact]
        public void Reflector_InitNullComposition_InitializesChildInstances()
        {
            var emp = new Employee { Credential = null };
            Reflector.InitNullComposition(emp);

            Assert.NotNull(emp.Credential);
        }

        [Fact]
        public void Reflector_InitNullComposition_NullSource_IsNoOp()
        {
            Reflector.InitNullComposition(null);
        }

        [Fact]
        public void Reflector_CloneObjectData_CopiesScalarProperties()
        {
            var source = _testData[0];
            var dest = new Person();

            Reflector.CloneObjectData(source, dest);

            Assert.Equal("Carlos Silva", dest.Name);
            Assert.Equal("São Paulo", dest.City);
            Assert.Equal(35m, dest.Age);
        }

        [Fact]
        public void Reflector_CloneObjectData_NullDestination_CreatesInstance()
        {
            var source = _testData[0];

            Reflector.CloneObjectData(source, null);
        }

        [Fact]
        public void Reflector_CloneObjectData_DeepClonesListsAndCompositions()
        {
            var source = new Order
            {
                Id = 10,
                Description = "Pedido",
                Lines = new List<OrderLine>
                {
                    new OrderLine { Id = 1, Product = "A", Quantity = 2 },
                    new OrderLine { Id = 2, Product = "B", Quantity = 3 },
                },
                Metadata = new Credential { Logon = "ops.logon" },
            };
            var dest = new Order();

            Reflector.CloneObjectData(source, dest);

            Assert.Equal("Pedido", dest.Description);
            Assert.Equal(2, dest.Lines.Count);
            Assert.Equal("B", dest.Lines[1].Product);
            Assert.Equal("ops.logon", dest.Metadata.Logon);
        }

        [Fact]
        public void Reflector_GetObjectProps_ReturnsAllPublicProps()
        {
            var props = Reflector.GetObjectProps(_testData[0]);

            Assert.Equal(7, props.Length);
        }

        [Fact]
        public void Reflector_GetObjectProps_FilterByName()
        {
            var props = Reflector.GetObjectProps(_testData[0], "Name", "City");

            Assert.Equal(2, props.Length);
            Assert.Contains(props, p => p.Name == "Name");
        }

        [Fact]
        public void Reflector_GetObjectProps_ChildNavigationFilter()
        {
            var props = Reflector.GetObjectProps(_employeeData()[0], "Credential.Logon");

            Assert.Equal(1, props.Length);
            Assert.Equal("Logon", props[0].Name);
        }

        [Fact]
        public void Reflector_GetObjectProps_ChildFilterWithNullInstance()
        {
            var props = Reflector.GetObjectProps(new Employee { Credential = null }, "Credential.Logon");

            Assert.Equal(1, props.Length);
            Assert.Equal("Logon", props[0].Name);
        }

        [Fact]
        public void Reflector_GetObjectProps_UnknownFilter_ReturnsEmpty()
        {
            var props = Reflector.GetObjectProps(_testData[0], "NotARealProp");

            Assert.Empty(props);
        }

        [Fact]
        public void Reflector_GetObjectPropValues_ReturnsValuesArray()
        {
            var props = Reflector.GetObjectProps(_testData[0], "Name", "Age");
            var values = Reflector.GetObjectPropValues(_testData[0], props);

            Assert.Equal("Carlos Silva", values[0]);
            Assert.Equal(35m, values[1]);
        }

        [Fact]
        public void Reflector_GetTypedValue_CoversAllScalarTypes()
        {
            Assert.Null(Reflector.GetTypedValue(typeof(string), null));
            Assert.Null(Reflector.GetTypedValue(typeof(string), DBNull.Value));
            Assert.Equal((short)5, Reflector.GetTypedValue(typeof(short), "5"));
            Assert.Equal(5, Reflector.GetTypedValue(typeof(int), "5"));
            Assert.Equal(5L, Reflector.GetTypedValue(typeof(long), "5"));
            Assert.Equal(5m, Reflector.GetTypedValue(typeof(decimal), "5"));
            Assert.Equal(5d, Reflector.GetTypedValue(typeof(double), "5"));
            Assert.Equal(5f, Reflector.GetTypedValue(typeof(float), "5"));
            Assert.Equal(5f, Reflector.GetTypedValue(typeof(Single), "5"));
            Assert.True((bool)Reflector.GetTypedValue(typeof(bool), "true"));
            Assert.Equal("abc", Reflector.GetTypedValue(typeof(string), "abc"));
            Assert.Equal(new DateTime(2020, 5, 1), Reflector.GetTypedValue(typeof(DateTime), "2020-05-01"));
            Assert.Equal(42, Reflector.GetTypedValue(typeof(object), 42));
        }

        [Fact]
        public void Reflector_GetInternalChilds_ReturnsChildInstances()
        {
            var method = typeof(Reflector).GetMethod("getObjectChilds",
                BindingFlags.Static | BindingFlags.NonPublic);
            var result = (object[])method.Invoke(null, new object[] { new Employee() });

            Assert.NotNull(result);
            Assert.Contains(result, r => r is Credential);
        }

        [Fact]
        public void ReflectorGeneric_Clone_ReturnsTypedClone()
        {
            var clone = Reflector<Person>.CloneObjectData(_testData[0]);

            Assert.Equal("Carlos Silva", clone.Name);
            Assert.Equal(5000m, clone.CreditLimit);
        }

        #endregion

        #region Serializer coverage

        [Fact]
        public void Serializer_SerializeCSV_WritesHeaderAndRows()
        {
            var csv = Serializer.SerializeCSV(new List<Person> { _testData[0], _testData[1] });
            var lines = csv.Trim().Split(Environment.NewLine);

            Assert.Contains("Name;City", lines[0]);
            Assert.Equal(3, lines.Length);
            Assert.Contains("Carlos Silva", csv);
        }

        [Fact]
        public void Serializer_SerializeCSV_EmptyList_ReturnsEmpty()
        {
            Assert.Equal("", Serializer.SerializeCSV(new List<Person>()));
        }

        [Fact]
        public void Serializer_SerializeCSV_NullSource_ReturnsEmpty()
        {
            Assert.Equal("", Serializer.SerializeCSV(null));
        }

        [Fact]
        public void Serializer_DeserializeCSV_RoundTripsPerson()
        {
            var csv = "Id;Name;City;State;Age;Active;CreditLimit\r\n"
                    + "1;Carlos;Sao Paulo;SP;35;True;5000\r\n"
                    + "2;Ana;Rio;RJ;28;True;3000\r\n";
            var items = Serializer.DeserializeCSV(csv, typeof(Person));

            Assert.Equal(2, items.Length);

            var first = (Person)items[0];
            Assert.Equal(1m, first.Id);
            Assert.Equal("Carlos", first.Name);
            Assert.Equal(35m, first.Age);
            Assert.True(first.Active);
            Assert.Equal(5000m, first.CreditLimit);
        }

        [Fact]
        public void Serializer_DeserializeCSV_ShortRow_IgnoresMissingColumns()
        {
            var csv = "Id;Name;Age\r\n1;Carlos;35\r\n2;Ana\r\n";
            var items = Serializer.DeserializeCSV(csv, typeof(Person));

            Assert.Equal(2, items.Length);
            Assert.Equal(0m, ((Person)items[1]).Age);
        }

        #endregion

        #region BitWiseTable coverage

        [Fact]
        public void Table_GetTable_ReturnsOrderedMasks()
        {
            var table = BitWiseTable.GetTable(typeof(Person));

            Assert.Equal(7, table.Count());
            var nameEntry = table.Single(e => e.Key.Name == "Name");
            Assert.Equal(BigInteger.One << 1, nameEntry.Value);
        }

        [Fact]
        public void Table_IndexOf_FoundAndMissing()
        {
            Assert.Equal(1, BitWiseTable.IndexOf(typeof(Person), "Name"));
            Assert.Equal(-1, BitWiseTable.IndexOf(typeof(Person), "MissingProp"));
        }

        [Fact]
        public void Table_GetMask_ReturnsMaskOrZero()
        {
            Assert.Equal(BigInteger.One << 2, BitWiseTable.GetMask(typeof(Person), "City"));
            Assert.Equal(BigInteger.Zero, BitWiseTable.GetMask(typeof(Person), "MissingProp"));
        }

        #endregion
    }
}