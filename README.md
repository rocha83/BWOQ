# Rochas.BWOQ

[English](#english) | [Português](#português) | [Español](#español) | [Français](#français) | [Deutsch](#deutsch)

---

## English

# README - BWOQ (Rochas.BWOQ)

**BWOQ (BitWise Object Query)** is a component for compact composition of queries on in-memory object collections, using **bitwise notation** and **postfix operators** (reverse Polish notation).

Each object attribute is indexed in a **binary table** at runtime and, with the logical disjunction of the indices (predicates) and the combination of criteria, you compose **Select**, **Where**, **OrderBy / OrderByDescending** and **GroupBy** with very little writing — plus **JSON** or **CSV** output of the results.

---

## 📌 Installation

```bash
dotnet add package Rochas.BWOQ
```

---

## 📌 Class Names

```text
BitWiseQuery<T>   --> Query engine (long methods + aliases Q, W, O, OD, G)
BWQFilter<T>      --> Chainable builder, implements IQueryable<T> / IEnumerable<T>
```

## 📋 BWOQ Syntax — Quick Reference

| Operation | Syntax | Example | Description |
| --------- | ------ | ------- | ----------- |
| **Select (Q)** | `<mask>` | `Q("6")` | Projects Name (2) + City (4) = 6 |
| **Where (W)** — like | `<mask>::<value>` | `W("2::silva")` | String similarity, case-insensitive |
| Equality | `<mask>::<value>=` | `W("32::1=")` | `=`, with `&` = AND between columns (`32::1&=`) |
| Greater than | `<mask>::<value>+` | `W("16::40+")` | `>` (in aggregation: Max) |
| Less than | `<mask>::<value>-` | `W("16::30-")` | `<` (in aggregation: Min) |
| Greater or equal | `<mask>::<value>=+` | `W("16::35=+")` | `>=` |
| Less or equal | `<mask>::<value>=-` | `W("16::35=-")` | `<=` |
| Internal comparison | `<mask>::<value><[&]<suffix>` | `W("6::10<-")` | Compares two columns (even mask) against each other; `<` switches to this mode, the trailing suffix sets the operator (`=`/`+`/`-`/`=+`/`=-`), `&` = AND between pairs |
| **OrderBy (O)** | `<mask>` | `O("2")` | Ascending by Name (2) |
| **OrderByDescending (OD)** | `<mask>` | `OD("64")` | Descending by CreditLimit (64) |
| **GroupBy (G)** | `<grp>, <by>` | `G("4", "4")` | Groups by City (4); `grp` may carry an aggregation suffix |
| Count | `<mask>*` | `G("4*", "4")` | `CountResult` per group |
| Sum | `<mask>^` | `G("64^", "4")` | `SumOfCreditLimits` |
| Average | `<mask>~` | `G("16~", "4")` | `AverageOfAges` |
| Max | `<mask>+` | `G("64+", "4")` | `MaximumOfCreditLimits` |
| Min | `<mask>-` | `G("64-", "4")` | `MinimumOfCreditLimits` |
| Navigation | `<mask>>ordinal:mask` | `Q("3>1:2")` | Projects `Credential.Logon` (aggregate at ordinal 1) |

---

## 📌 Entity and Binary Table Example

Each attribute receives a power of 2 in declaration order:

```csharp
public class Person
{
    public decimal Id { get; set; }          // 1
    public string Name { get; set; }         // 2
    public string City { get; set; }         // 4
    public string State { get; set; }        // 8
    public decimal Age { get; set; }         // 16
    public bool Active { get; set; }         // 32
    public decimal CreditLimit { get; set; } // 64
}
```

The **combination** (binary sum) identifies a set of attributes:

```text
Name + City        = 2 + 4  = 6
Age + Active       = 16 + 32 = 48
all columns        = 1+2+4+8+16+32+64 = 127
```

> 💡 Boolean values can be entered as `1` (true) / `0` (false)
> or literally `true` / `false`.

---

## ⚡ Q — Column Selection (Projection)

```csharp
var bwq = new BitWiseQuery<Person>(personList.AsQueryable());

// Projection of Name (2) + City (4) only
var projected = bwq.Query("6", standAlone: true);

// All columns (127), filter mode (chainable)
var all = bwq.Q("127");
```

---

## 🔍 W — Filters (Criteria)

Syntax: `#::value`  →  `#` is the binary of the attribute(s) and `value` is the criterion.

### Equality — `=` (and conjunction `&`)

```csharp
// Active (32) = true  (value 1 + Equality, using conjunction &)
var active = bwq.W("32::1&=");

// Pure equality to 1
var active2 = bwq.W("32::1=");
```

> The `&` token applies the **And conjunction** to the criterion. When selecting more than one attribute
> in the binary (e.g.: `6` = Name + City), the same value and comparison are applied to all of them
> — without `&` the behavior is disjunction `Or`.

### Similarity 'like' — pattern for strings (case-insensitive)

```csharp
// Name contains "Silva"
var byName = bwq.W("2::silva");

// City contains "paulo" (3 people from São Paulo)
var byCity = bwq.W("4::paulo");
```

### Numerical Comparators

```csharp
var older = bwq.W("16::40+");     // Age >  40
var younger = bwq.W("16::30-");   // Age <  30
var olderOrEqual = bwq.W("16::35=+");  // Age >= 35
var upTo35 = bwq.W("16::35=-");   // Age <= 35
```

---

## ✏️ O / OD — Sorting

```csharp
// Ascending by Name (2) of active
var sorted = bwq.Q("127").W("32::1&=").O("2");

// Descending by CreditLimit (64)
var sortedDesc = bwq.Q("127").W("32::1&=").OD("64");
```

---

## 🧮 G — Grouping

```csharp
// Groups by City (4), returning groups with Key
var groups = bwq.G("4", "4");

// Groups by City (4) of active only
var groupsActive = bwq.Q("127").W("32::1&=").G("4", "4");
```

---

## ∑ Aggregations (postfix suffixes)

Aggregations operate on the column specified in the binary, grouped by `by`:

| Suffix | Operation | Generated field in result |
| ------ | --------- | ------------------------- |
| `*`    | Count     | `CountResult`             |
| `^`    | Sum       | `SumOf<Atribute>s`        |
| `~`    | Average   | `AverageOf<Atribute>s`    |
| `+`    | Max       | `MaximumOf<Atribute>s`    |
| `-`    | Min       | `MinimumOf<Atribute>s`    |

```csharp
// Count of active by City
var countByCity = bwq.Q("127").W("32::1&=").G("4*", "4");   // { City: CountResult }

// Sum of CreditLimit (64 / active) by City
var sumByCity = bwq.Q("127").W("32::1&=").G("64^", "4");    // { City: SumOfCreditLimits }

// Average of Age (16 / active) by City
var avgByCity = bwq.Q("127").W("32::1&=").G("16~", "4");    // { City: AverageOfAges }

// Max and Min of CreditLimit by City
var maxByCity = bwq.Q("127").W("32::1&=").G("64+", "4");    // { City: MaximumOfCreditLimits }
var minByCity = bwq.Q("127").W("32::1&=").G("64-", "4");    // { City: MinimumOfCreditLimits }
```

---

## 🔗 Navigation in Aggregated Objects

Attributes that are **classes from the same module** are excluded from the flat binary table and accessed
by navigation `>position:mask`.

```csharp
public class Employee
{
    public decimal Id { get; set; }             // 1
    public string Name { get; set; }            // 2
    public decimal Age { get; set; }            // 4
    public bool Active { get; set; }            // 8

    public Credential Credential { get; set; }  // aggregate (ordinal position 1)
}

public class Credential
{
    public decimal Id { get; set; }             // 1
    public string Logon { get; set; }           // 2
    public string TokenId { get; set; }         // 4
}
```

### Navigation in Predicate — `Q`

```csharp
var empBwq = new BitWiseQuery<Employee>(employeeList.AsQueryable());

// Id + Name (3) of Employee + Logon (2) of Credential
var proj = empBwq.Q("3>1:2", true);

// Active (8) + TokenId (4) of Credential
var proj2 = empBwq.Q("8>1:4", true);

// Age (4) + all Credential fields (7)
var projDeep = empBwq.Q("4>1:7", true);
```

### Navigation in Filter — `W`

```csharp
// Name (2) of Employee + Logon (2) of Credential, both by like of "ana"
var byCredential = empBwq.Q("15").W("2>1:2::ana");
```

---

## 🔗 Chainable Builder

The `BWQFilter<T>` implements `IQueryable<T>`; just chain and enumerate:

```csharp
var result = bwq.Q("127")
                .W("32::1&=")
                .O("2")
                .ToList();            // List<Person>

foreach (var person in result)
    Console.WriteLine($"{person.Name} - {person.City}");
```

---

## 📄 JSON / CSV Outputs

```csharp
var json = bwq.Where("32::1&=", EnumSerialDataType.JSON);
var csv  = bwq.Where("32::1&=", EnumSerialDataType.CSV);
```

---

## ❓ Syntax Reference

### Predicate (attribute selection)

| Token  | Meaning                                         |
| ------ | ----------------------------------------------- |
| `N`    | Binary combination of selected attributes       |
| `>`    | Start of navigation to aggregate object          |
| `N`    | Ordinal position of the aggregate                |
| `:`    | Token to 'get' the attributes of the aggregate   |
| `M`    | Binary of selected attributes from the aggregate |
| `*`    | Count                                           |
| `^`    | Sum                                             |
| `~`    | Average                                         |
| `+`    | Max                                             |
| `-`    | Min                                             |

### Criterion (Where filter)

| Token          | Example          | Meaning                             |
| -------------- | ---------------- | ----------------------------------- |
| `#::`          | `32::1&=`        | 'where the value is'                |
| `&`            | `32::1&=`        | **And** conjunction (default: `Or`) |
| `=`            | `32::1=`         | Equality                            |
| *(default)*    | `2::carlos`      | Similarity ('like') for strings     |
| `+`            | `16::40+`        | Greater than (in aggregation: Max)  |
| `-`            | `16::30-`        | Less than (in aggregation: Min)     |
| `=+`           | `16::35=+`       | Greater or equal                    |
| `=-`           | `16::35=-`       | Less or equal                       |

---

## 🔧 Available Methods

### BitWiseQuery\<T\> (query engine)

| Method                        | Alias              | Return                |
| ----------------------------- | ------------------ | --------------------- |
| `Query(string)`               | `Q(string)`        | `BWQFilter<T>` (builder) |
| `Query(string, bool)`         | `Q(string, standAlone)` | `IQueryable` (projection) |
| `Query(string, EnumSerialDataType)` | `Q(...)`       | `string` (JSON/CSV) |
| `Where(string)`               | `W(string)`        | `IQueryable`      |
| `Where(string, bool)`         | `W(string, hasSufix)` | `BWQFilter<T>` |
| `Where(string, EnumSerialDataType)` | `W(...)`     | `string` (JSON/CSV) |
| `OrderBy(string)`             | `O(string)`        | `IQueryable`   |
| `OrderByDescending(string)`   | `OD(string)`       | `IQueryable`   |
| `GroupBy(string, string)`     | `G(by, grp)`       | `IQueryable`   |

### BWQFilter\<T\> (builder)

| Method          | Return      |
| --------------- | ----------- |
| `W(string)`     | `BWQFilter<T>` |
| `O(string)`     | `BWQFilter<T>` |
| `OD(string)`    | `BWQFilter<T>` |
| `G(string, string)` | `IQueryable` |

---

## 🧪 Tests

The `Rochas.BWOQ.Test` package covers (**56 tests passed / 0 failures**):

- Column projection (`Q(..., true)`) and aggregate navigation;
- Filters: `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, including aggregate attributes and internal comparison (`<`);
- Sorting `O` / `OD`;
- Chainable builder `Q().W().O()/OD()`;
- Grouping `G` and aggregations `Count`, `Sum`, `Average`, `Max`, `Min`;
- `IList` and `ref` constructors; `JSON` / `CSV` serialization overloads;
- Base→derived inheritance with `BigInteger` masks (> bit 31);
- `BitWiseTable`, `Reflector` (clone/projection/typing) and `Serializer` (CSV) helpers;
- Custom exception messages.

### Code coverage (assembly Rochas.BWOQ, coverlet)

| Metric           | Value  |
| ----------------- | ------ |
| Lines             | 95.19% |
| Branches          | 97.56% |

| Type                                        | Coverage |
| ------------------------------------------- | -------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95.3% / 100% |
| Helpers: BitWiseTable, Reflector, Serializer | 100% / 87.5% / 88% |
| Custom exceptions (`Invalid*Exception`)     | 100% |

> The `Reflector` implementation (clone/projection/typing) was moved to the `Rochas.SqlWrapper` package (`EntityReflector`); here the `Reflector` remains only as a delegation facade.
---

## Português

# README - BWOQ (Rochas.BWOQ)

**BWOQ (BitWise Object Query)** é um componente para composição compacta de consultas sobre coleções de objetos em memória, usando **notação bitwise** e **operadores pósfixados** (notação polonesa reversa).

Cada atributo do objeto é indexado em uma **tabela binária** em tempo de execução e, com a disjunção lógica dos índices (predicados) e a combinação de critérios, você compõe **Select**, **Where**, **OrderBy / OrderByDescending** e **GroupBy** com pouquíssima escrita — além da saída **JSON** ou **CSV** dos resultados.

---

## 📌 Instalação

```bash
dotnet add package Rochas.BWOQ
```

---

## 📌 Nome das Classes

```text
BitWiseQuery<T>   --> Motor de consulta (métodos longos + aliases Q, W, O, OD, G)
BWQFilter<T>      --> Builder encadeável, implementa IQueryable<T> / IEnumerable<T>
```

## 📋 Sintaxe BWOQ — Referência Rápida

| Operação | Sintaxe | Exemplo | Descrição |
| -------- | ------- | ------- | --------- |
| **Select (Q)** | `<máscara>` | `Q("6")` | Projeta Name (2) + City (4) = 6 |
| **Where (W)** — like | `<máscara>::<valor>` | `W("2::silva")` | Semelhança em strings, case-insensitive |
| Igualdade | `<máscara>::<valor>=` | `W("32::1=")` | `=`, com `&` = conjunção AND entre colunas (`32::1&=`) |
| Maior que | `<máscara>::<valor>+` | `W("16::40+")` | `>` (na agregação: Max) |
| Menor que | `<máscara>::<valor>-` | `W("16::30-")` | `<` (na agregação: Min) |
| Maior ou igual | `<máscara>::<valor>=+` | `W("16::35=+")` | `>=` |
| Menor ou igual | `<máscara>::<valor>=-` | `W("16::35=-")` | `<=` |
| Comparação interna | `<máscara>::<valor><[&]<sufixo>` | `W("6::10<-")` | Compara duas colunas (máscara par) entre si; o `<` ativa este modo, o sufixo final define o operador (`=`/`+`/`-`/`=+`/`=-`), `&` = AND entre os pares |
| **OrderBy (O)** | `<máscara>` | `O("2")` | Ascendente por Name (2) |
| **OrderByDescending (OD)** | `<máscara>` | `OD("64")` | Descendente por CreditLimit (64) |
| **GroupBy (G)** | `<agr>, <by>` | `G("4", "4")` | Agrupa por City (4); `agr` pode ter sufixo de agregação |
| Count | `<máscara>*` | `G("4*", "4")` | `CountResult` por grupo |
| Sum | `<máscara>^` | `G("64^", "4")` | `SumOfCreditLimits` |
| Média | `<máscara>~` | `G("16~", "4")` | `AverageOfAges` |
| Max | `<máscara>+` | `G("64+", "4")` | `MaximumOfCreditLimits` |
| Min | `<máscara>-` | `G("64-", "4")` | `MinimumOfCreditLimits` |
| Navegação | `<máscara>>ordinal:máscara` | `Q("3>1:2")` | Projeta `Credential.Logon` (agregado no ordinal 1) |

---

## 📌 Exemplo de Entidade e Tabela Binária

Cada atributo recebe uma potência de 2 na ordem de declaração:

```csharp
public class Person
{
    public decimal Id { get; set; }          // 1
    public string Name { get; set; }         // 2
    public string City { get; set; }         // 4
    public string State { get; set; }        // 8
    public decimal Age { get; set; }         // 16
    public bool Active { get; set; }         // 32
    public decimal CreditLimit { get; set; } // 64
}
```

A **combinação** (soma binária) identifica um conjunto de atributos:

```text
Name + City        = 2 + 4  = 6
Age + Active       = 16 + 32 = 48
todas as colunas   = 1+2+4+8+16+32+64 = 127
```

> 💡 Valores booleano por binário podem ser informados como `1` (true) / `0` (false)
> ou literalmente `true` / `false`.

---

## ⚡ Q — Seleção de Colunas (Projeção)

```csharp
var bwq = new BitWiseQuery<Person>(personList.AsQueryable());

// Projeção apenas de Name (2) + City (4)
var projected = bwq.Query("6", standAlone: true);

// Todas as colunas (127), modo filtro (chainable)
var all = bwq.Q("127");
```

---

## 🔍 W — Filtros (Critérios)

Sintaxe: `#::valor`  →  `#` é o binário do(s) atributo(s) e `valor` é o critério.

### Igualdade — `=` (e conjunção `&`)

```csharp
// Active (32) = true  (valor 1 + Igualdade, usando conjunção &)
var ativos = bwq.W("32::1&=");

// Igualdade pura a 1
var ativos2 = bwq.W("32::1=");
```

> O token `&` aplica a **conjunção And** ao critério. Ao selecionar mais de um atributo
> no binário (ex.: `6` = Name + City), o mesmo valor e comparação são aplicados a todos eles
> — sem `&` o comportamento é disjunção `Or`.

### Semelhança 'like' — padrão para strings (case-insensitive)

```csharp
// Name contém "Silva"
var byName = bwq.W("2::silva");

// City contém "paulo" (3 pessoas de São Paulo)
var byCity = bwq.W("4::paulo");
```

### Comparadores numéricos

```csharp
var maisVelhos = bwq.W("16::40+");   // Age >  40
var maisNovos  = bwq.W("16::30-");   // Age <  30
var maiores    = bwq.W("16::35=+");  // Age >= 35
var ate35      = bwq.W("16::35=-");  // Age <= 35
```

---

## ✏️ O / OD — Ordenação

```csharp
// Ascendente por Name (2) dos ativos
var sorted = bwq.Q("127").W("32::1&=").O("2");

// Descendente por CreditLimit (64)
var sortedDesc = bwq.Q("127").W("32::1&=").OD("64");
```

---

## 🧮 G — Agrupamento

```csharp
// Agrupa por City (4), retornando os grupos com a Key
var groups = bwq.G("4", "4");

// Agrupa por City (4) somente dos ativos
var groupsActive = bwq.Q("127").W("32::1&=").G("4", "4");
```

---

## ∑ Agregações (sufixos pósfixados)

Agregações operam sobre a coluna informada no binário, agrupada pelo `by`:

| Sufixo | Operação  | Campo gerado no resultado     |
| ------ | --------- | ----------------------------- |
| `*`    | Count     | `CountResult`                 |
| `^`    | Sum       | `SumOf<Atributo>s`            |
| `~`    | Average   | `AverageOf<Atributo>s`        |
| `+`    | Max       | `MaximumOf<Atributo>s`        |
| `-`    | Min       | `MinimumOf<Atributo>s`        |

```csharp
// Contagem de ativos por City
var countByCity = bwq.Q("127").W("32::1&=").G("4*", "4");   // { City: CountResult }

// Soma de CreditLimit (64 / ativos) por City
var sumByCity = bwq.Q("127").W("32::1&=").G("64^", "4");    // { City: SumOfCreditLimits }

// Média de Age (16 / ativos) por City
var avgByCity = bwq.Q("127").W("32::1&=").G("16~", "4");    // { City: AverageOfAges }

// Máx e Mín de CreditLimit por City
var maxByCity = bwq.Q("127").W("32::1&=").G("64+", "4");    // { City: MaximumOfCreditLimits }
var minByCity = bwq.Q("127").W("32::1&=").G("64-", "4");    // { City: MinimumOfCreditLimits }
```

---

## 🔗 Navegação em Objetos Agregados

Atributos que são **classes do mesmo módulo** são excluídos da tabela binária plana e acessados
por navegação `>posição:máscara`.

```csharp
public class Employee
{
    public decimal Id { get; set; }             // 1
    public string Name { get; set; }            // 2
    public decimal Age { get; set; }            // 4
    public bool Active { get; set; }            // 8

    public Credential Credential { get; set; }  // agregado (posição ordinal 1)
}

public class Credential
{
    public decimal Id { get; set; }             // 1
    public string Logon { get; set; }           // 2
    public string TokenId { get; set; }         // 4
}
```

### Navegação no Predicado — `Q`

```csharp
var empBwq = new BitWiseQuery<Employee>(employeeList.AsQueryable());

// Id + Name (3) do Employee + Logon (2) do Credential
var proj = empBwq.Q("3>1:2", true);

// Ativos (8) + TokenId (4) do Credential
var proj2 = empBwq.Q("8>1:4", true);

// Age (4) + todos os campos do Credential (7)
var projDeep = empBwq.Q("4>1:7", true);
```

### Navegação no Filtro — `W`

```csharp
// Name (2) do Employee + Logon (2) do Credential, ambos por like de "ana"
var byCredential = empBwq.Q("15").W("2>1:2::ana");
```

---

## 🔗 Builder Encadeado

O `BWQFilter<T>` implementa `IQueryable<T>`; basta encadear e enumerar:

```csharp
var result = bwq.Q("127")
                .W("32::1&=")
                .O("2")
                .ToList();            // List<Person>

foreach (var person in result)
    Console.WriteLine($"{person.Name} - {person.City}");
```

---

## 📄 Saídas JSON / CSV

```csharp
var json = bwq.Where("32::1&=", EnumSerialDataType.JSON);
var csv  = bwq.Where("32::1&=", EnumSerialDataType.CSV);
```

---

## ❓ Referência de Sintaxe

### Predicado (seleção de atributos)

| Token  | Significado                                     |
| ------ | ----------------------------------------------- |
| `N`    | Combinação binária dos atributos selecionados    |
| `>`    | Início da navegação para objeto agregado          |
| `N`    | Posição ordinal do agregado                      |
| `:`    | Token 'obter' os atributos do agregado            |
| `M`    | Binário dos atributos selecionados do agregado    |
| `*`    | Count (contagem)                                 |
| `^`    | Sum (soma)                                       |
| `~`    | Average (média)                                  |
| `+`    | Max                                              |
| `-`    | Min                                              |

### Critério (filtro Where)

| Token          | Exemplo          | Significado                          |
| -------------- | ---------------- | ------------------------------------ |
| `#::`          | `32::1&=`        | 'onde o valor é'                     |
| `&`            | `32::1&=`        | Conjunção **And** (padrão: `Or`)      |
| `=`            | `32::1=`         | Igualdade                            |
| *(default)*    | `2::carlos`      | Semelhança ('like') para strings      |
| `+`            | `16::40+`        | Maior que (na agregação: Max)          |
| `-`            | `16::30-`        | Menor que (na agregação: Min)          |
| `=+`           | `16::35=+`       | Maior ou igual                        |
| `=-`           | `16::35=-`       | Menor ou igual                        |

---

## 🔧 Métodos Disponíveis

### BitWiseQuery\<T\> (motor de consulta)

| Método                        | Alias              | Retorno               |
| ----------------------------- | ------------------ | --------------------- |
| `Query(string)`               | `Q(string)`        | `BWQFilter<T>` (builder) |
| `Query(string, bool)`         | `Q(string, standAlone)` | `IQueryable` (projeção) |
| `Query(string, EnumSerialDataType)` | `Q(...)`       | `string` (JSON/CSV) |
| `Where(string)`               | `W(string)`        | `IQueryable`      |
| `Where(string, bool)`         | `W(string, hasSufix)` | `BWQFilter<T>` |
| `Where(string, EnumSerialDataType)` | `W(...)`     | `string` (JSON/CSV) |
| `OrderBy(string)`             | `O(string)`        | `IQueryable`   |
| `OrderByDescending(string)`   | `OD(string)`       | `IQueryable`   |
| `GroupBy(string, string)`     | `G(by, grp)`       | `IQueryable`   |

### BWQFilter\<T\> (builder)

| Método          | Retorno     |
| --------------- | ----------- |
| `W(string)`     | `BWQFilter<T>` |
| `O(string)`     | `BWQFilter<T>` |
| `OD(string)`    | `BWQFilter<T>` |
| `G(string, string)` | `IQueryable` |

---

## 🧪 Testes

O pacote `Rochas.BWOQ.Test` cobre (**56 testes aprovados / 0 falhas**):

- Projeção de colunas (`Q(..., true)`) e navegação em agregados;
- Filtros: `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, inclusive em atributos de agregado e comparação interna (`<`);
- Ordenação `O` / `OD`;
- Builder encadeado `Q().W().O()/OD()`;
- Agrupamento `G` e agregações `Count`, `Sum`, `Average`, `Max`, `Min`;
- Construtores de `IList` e `ref`; overloads de serialização `JSON` / `CSV`;
- Herança base→derivada com máscaras `BigInteger` (> bit 31);
- Helpers `BitWiseTable`, `Reflector` (clone/projeção/tipagem) e `Serializer` (CSV);
- Mensagens das exceções customizadas.

### Cobertura de código (assembly Rochas.BWOQ, coverlet)

| Métrica          | Valor  |
| ----------------- | ------ |
| Linhas            | 95,19% |
| Branches          | 97,56% |

| Tipo                                        | Cobertura |
| ------------------------------------------- | --------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95,3% / 100% |
| Helpers: BitWiseTable, Reflector, Serializer | 100% / 87,5% / 88% |
| Exceções customizadas (`Invalid*Exception`)  | 100% |

> A implementação do `Reflector` (clone/projeção/tipagem) foi movida para o pacote `Rochas.SqlWrapper` (`EntityReflector`); aqui o `Reflector` permanece apenas como fachada de delegação.
---

## Español

# README - BWOQ (Rochas.BWOQ)

**BWOQ (BitWise Object Query)** es un componente para la composición compacta de consultas sobre colecciones de objetos en memoria, utilizando **notación bitwise** y **operadores postfijados** (notación polaca inversa).

Cada atributo del objeto se indexa en una **tabla binaria** en tiempo de ejecución y, con la disyunción lógica de los índices (predicados) y la combinación de criterios, usted compone **Select**, **Where**, **OrderBy / OrderByDescending** y **GroupBy** con muy poca escritura — además de la salida **JSON** o **CSV** de los resultados.

---

## 📌 Instalación

```bash
dotnet add package Rochas.BWOQ
```

---

## 📌 Nombres de las Clases

```text
BitWiseQuery<T>   --> Motor de consulta (métodos largos + alias Q, W, O, OD, G)
BWQFilter<T>      --> Constructor encadenable, implementa IQueryable<T> / IEnumerable<T>
```

## 📋 Sintaxis BWOQ — Referencia Rápida

| Operación | Sintaxis | Ejemplo | Descripción |
| --------- | -------- | ------- | ----------- |
| **Select (Q)** | `<máscara>` | `Q("6")` | Proyecta Name (2) + City (4) = 6 |
| **Where (W)** — like | `<máscara>::<valor>` | `W("2::silva")` | Similitud en strings, insensible a mayúsculas |
| Igualdad | `<máscara>::<valor>=` | `W("32::1=")` | `=`, con `&` = conjunción AND entre columnas (`32::1&=`) |
| Mayor que | `<máscara>::<valor>+` | `W("16::40+")` | `>` (en la agregación: Max) |
| Menor que | `<máscara>::<valor>-` | `W("16::30-")` | `<` (en la agregación: Min) |
| Mayor o igual | `<máscara>::<valor>=+` | `W("16::35=+")` | `>=` |
| Menor o igual | `<máscara>::<valor>=-` | `W("16::35=-")` | `<=` |
| Comparación interna | `<máscara>::<valor><[&]<sufijo>` | `W("6::10<-")` | Compara dos columnas (máscara par) entre sí; el `<` activa este modo, el sufijo final define el operador (`=`/`+`/`-`/`=+`/`=-`), `&` = AND entre los pares |
| **OrderBy (O)** | `<máscara>` | `O("2")` | Ascendente por Name (2) |
| **OrderByDescending (OD)** | `<máscara>` | `OD("64")` | Descendente por CreditLimit (64) |
| **GroupBy (G)** | `<agr>, <by>` | `G("4", "4")` | Agrupa por City (4); `agr` puede llevar sufijo de agregación |
| Count | `<máscara>*` | `G("4*", "4")` | `CountResult` por grupo |
| Sum | `<máscara>^` | `G("64^", "4")` | `SumOfCreditLimits` |
| Promedio | `<máscara>~` | `G("16~", "4")` | `AverageOfAges` |
| Max | `<máscara>+` | `G("64+", "4")` | `MaximumOfCreditLimits` |
| Min | `<máscara>-` | `G("64-", "4")` | `MinimumOfCreditLimits` |
| Navegación | `<máscara>>ordinal:máscara` | `Q("3>1:2")` | Proyecta `Credential.Logon` (agregado en ordinal 1) |

---

## 📌 Ejemplo de Entidad y Tabla Binaria

Cada atributo recibe una potencia de 2 en el orden de declaración:

```csharp
public class Person
{
    public decimal Id { get; set; }          // 1
    public string Name { get; set; }         // 2
    public string City { get; set; }         // 4
    public string State { get; set; }        // 8
    public decimal Age { get; set; }         // 16
    public bool Active { get; set; }         // 32
    public decimal CreditLimit { get; set; } // 64
}
```

La **combinación** (suma binaria) identifica un conjunto de atributos:

```text
Name + City        = 2 + 4  = 6
Age + Active       = 16 + 32 = 48
todas las columnas = 1+2+4+8+16+32+64 = 127
```

> 💡 Los valores booleanos por binario pueden indicarse como `1` (true) / `0` (false)
> o literalmente `true` / `false`.

---

## ⚡ Q — Selección de Columnas (Proyección)

```csharp
var bwq = new BitWiseQuery<Person>(personList.AsQueryable());

// Proyección solo de Name (2) + City (4)
var projected = bwq.Query("6", standAlone: true);

// Todas las columnas (127), modo filtro (encadenable)
var all = bwq.Q("127");
```

---

## 🔍 W — Filtros (Criterios)

Sintaxis: `#::valor`  →  `#` es el binario de los atributos y `valor` es el criterio.

### Igualdad — `=` (y conjunción `&`)

```csharp
// Active (32) = true  (valor 1 + Igualdad, usando conjunción &)
var activos = bwq.W("32::1&=");

// Igualdad pura a 1
var activos2 = bwq.W("32::1=");
```

> El token `&` aplica la **conjunción And** al criterio. Al seleccionar más de un atributo
> en el binario (ej.: `6` = Name + City), el mismo valor y comparación se aplican a todos ellos
> — sin `&` el comportamiento es disyunción `Or`.

### Similitud 'like' — patrón para strings (case-insensitive)

```csharp
// Name contiene "Silva"
var byName = bwq.W("2::silva");

// City contiene "paulo" (3 personas de São Paulo)
var byCity = bwq.W("4::paulo");
```

### Comparadores numéricos

```csharp
var mayores = bwq.W("16::40+");     // Age >  40
var menores = bwq.W("16::30-");     // Age <  30
var mayoresOIgual = bwq.W("16::35=+");  // Age >= 35
var hasta35 = bwq.W("16::35=-");    // Age <= 35
```

---

## ✏️ O / OD — Ordenación

```csharp
// Ascendente por Name (2) de los activos
var sorted = bwq.Q("127").W("32::1&=").O("2");

// Descendente por CreditLimit (64)
var sortedDesc = bwq.Q("127").W("32::1&=").OD("64");
```

---

## 🧮 G — Agrupación

```csharp
// Agrupa por City (4), devolviendo los grupos con Key
var groups = bwq.G("4", "4");

// Agrupa por City (4) solo de los activos
var groupsActive = bwq.Q("127").W("32::1&=").G("4", "4");
```

---

## ∑ Agregaciones (sufijos postfijados)

Las agregaciones operan sobre la columna indicada en el binario, agrupada por `by`:

| Sufijo | Operación | Campo generado en el resultado |
| ------ | --------- | ------------------------------ |
| `*`    | Count     | `CountResult`                  |
| `^`    | Sum       | `SumOf<Atribute>s`             |
| `~`    | Average   | `AverageOf<Atribute>s`         |
| `+`    | Max       | `MaximumOf<Atribute>s`         |
| `-`    | Min       | `MinimumOf<Atribute>s`         |

```csharp
// Conteo de activos por City
var countByCity = bwq.Q("127").W("32::1&=").G("4*", "4");   // { City: CountResult }

// Suma de CreditLimit (64 / activos) por City
var sumByCity = bwq.Q("127").W("32::1&=").G("64^", "4");    // { City: SumOfCreditLimits }

// Promedio de Age (16 / activos) por City
var avgByCity = bwq.Q("127").W("32::1&=").G("16~", "4");    // { City: AverageOfAges }

// Máx y Mín de CreditLimit por City
var maxByCity = bwq.Q("127").W("32::1&=").G("64+", "4");    // { City: MaximumOfCreditLimits }
var minByCity = bwq.Q("127").W("32::1&=").G("64-", "4");    // { City: MinimumOfCreditLimits }
```

---

## 🔗 Navegación en Objetos Agregados

Los atributos que son **clases del mismo módulo** se excluyen de la tabla binaria plana y se acceden
por navegación `>posición:máscara`.

```csharp
public class Employee
{
    public decimal Id { get; set; }             // 1
    public string Name { get; set; }            // 2
    public decimal Age { get; set; }            // 4
    public bool Active { get; set; }            // 8

    public Credential Credential { get; set; }  // agregado (posición ordinal 1)
}

public class Credential
{
    public decimal Id { get; set; }             // 1
    public string Logon { get; set; }           // 2
    public string TokenId { get; set; }         // 4
}
```

### Navegación en el Predicado — `Q`

```csharp
var empBwq = new BitWiseQuery<Employee>(employeeList.AsQueryable());

// Id + Name (3) del Employee + Logon (2) del Credential
var proj = empBwq.Q("3>1:2", true);

// Activos (8) + TokenId (4) del Credential
var proj2 = empBwq.Q("8>1:4", true);

// Age (4) + todos los campos del Credential (7)
var projDeep = empBwq.Q("4>1:7", true);
```

### Navegación en el Filtro — `W`

```csharp
// Name (2) del Employee + Logon (2) del Credential, ambos por like de "ana"
var byCredential = empBwq.Q("15").W("2>1:2::ana");
```

---

## 🔗 Constructor Encadenable

El `BWQFilter<T>` implementa `IQueryable<T>`; solo encadena y enumera:

```csharp
var result = bwq.Q("127")
                .W("32::1&=")
                .O("2")
                .ToList();            // List<Person>

foreach (var person in result)
    Console.WriteLine($"{person.Name} - {person.City}");
```

---

## 📄 Salidas JSON / CSV

```csharp
var json = bwq.Where("32::1&=", EnumSerialDataType.JSON);
var csv  = bwq.Where("32::1&=", EnumSerialDataType.CSV);
```

---

## ❓ Referencia de Sintaxis

### Predicado (selección de atributos)

| Token  | Significado                                        |
| ------ | -------------------------------------------------- |
| `N`    | Combinación binaria de los atributos seleccionados  |
| `>`    | Inicio de navegación hacia objeto agregado           |
| `N`    | Posición ordinal del agregado                        |
| `:`    | Token para 'obtener' los atributos del agregado      |
| `M`    | Binario de los atributos seleccionados del agregado  |
| `*`    | Count                                              |
| `^`    | Sum                                                |
| `~`    | Average                                            |
| `+`    | Max                                                |
| `-`    | Min                                                |

### Criterio (filtro Where)

| Token          | Ejemplo          | Significado                         |
| -------------- | ---------------- | ----------------------------------- |
| `#::`          | `32::1&=`        | 'donde el valor es'                 |
| `&`            | `32::1&=`        | Conjunción **And** (predeterminado: `Or`) |
| `=`            | `32::1=`         | Igualdad                            |
| *(default)*    | `2::carlos`      | Similitud ('like') para strings     |
| `+`            | `16::40+`        | Mayor que (en agregación: Max)      |
| `-`            | `16::30-`        | Menor que (en agregación: Min)      |
| `=+`           | `16::35=+`       | Mayor o igual                       |
| `=-`           | `16::35=-`       | Menor o igual                       |

---

## 🔧 Métodos Disponibles

### BitWiseQuery\<T\> (motor de consulta)

| Método                        | Alias              | Retorno               |
| ----------------------------- | ------------------ | --------------------- |
| `Query(string)`               | `Q(string)`        | `BWQFilter<T>` (constructor) |
| `Query(string, bool)`         | `Q(string, standAlone)` | `IQueryable` (proyección) |
| `Query(string, EnumSerialDataType)` | `Q(...)`       | `string` (JSON/CSV) |
| `Where(string)`               | `W(string)`        | `IQueryable`      |
| `Where(string, bool)`         | `W(string, hasSufix)` | `BWQFilter<T>` |
| `Where(string, EnumSerialDataType)` | `W(...)`     | `string` (JSON/CSV) |
| `OrderBy(string)`             | `O(string)`        | `IQueryable`   |
| `OrderByDescending(string)`   | `OD(string)`       | `IQueryable`   |
| `GroupBy(string, string)`     | `G(by, grp)`       | `IQueryable`   |

### BWQFilter\<T\> (constructor)

| Método          | Retorno     |
| --------------- | ----------- |
| `W(string)`     | `BWQFilter<T>` |
| `O(string)`     | `BWQFilter<T>` |
| `OD(string)`    | `BWQFilter<T>` |
| `G(string, string)` | `IQueryable` |

---

## 🧪 Pruebas

El paquete `Rochas.BWOQ.Test` cubre (**56 pruebas aprobadas / 0 fallos**):

- Proyección de columnas (`Q(..., true)`) y navegación en agregados;
- Filtros: `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, incluyendo atributos de agregado y comparación interna (`<`);
- Ordenación `O` / `OD`;
- Constructor encadenable `Q().W().O()/OD()`;
- Agrupación `G` y agregaciones `Count`, `Sum`, `Average`, `Max`, `Min`;
- Constructores de `IList` y `ref`; sobrecargas de serialización `JSON` / `CSV`;
- Herencia base→derivada con máscaras `BigInteger` (> bit 31);
- Helpers `BitWiseTable`, `Reflector` (clone/proyección/tipado) y `Serializer` (CSV);
- Mensajes de excepciones personalizadas.

### Cobertura de código (assembly Rochas.BWOQ, coverlet)

| Métrica           | Valor  |
| ----------------- | ------ |
| Líneas            | 95.19% |
| Branches          | 97.56% |

| Tipo                                        | Cobertura |
| ------------------------------------------- | --------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95.3% / 100% |
| Helpers: BitWiseTable, Reflector, Serializer | 100% / 87.5% / 88% |
| Excepciones personalizadas (`Invalid*Exception`) | 100% |

> La implementación del `Reflector` (clone/proyección/tipado) fue movida al paquete `Rochas.SqlWrapper` (`EntityReflector`); aquí el `Reflector` permanece solo como fachada de delegación.
---

## Français

# README - BWOQ (Rochas.BWOQ)

**BWOQ (BitWise Object Query)** est un composant pour la composition compacte de requêtes sur des collections d'objets en mémoire, utilisant **la notation bitwise** et **les opérateurs postfixés** (notation polonaise inversée).

Chaque attribut de l'objet est indexé dans une **table binaire** au moment de l'exécution et, avec la disjonction logique des indices (prédicats) et la combinaison de critères, vous composez **Select**, **Where**, **OrderBy / OrderByDescending** et **GroupBy** avec très peu d'écriture — en plus de la sortie **JSON** ou **CSV** des résultats.

---

## 📌 Installation

```bash
dotnet add package Rochas.BWOQ
```

---

## 📌 Noms des Classes

```text
BitWiseQuery<T>   --> Moteur de requête (méthodes longues + alias Q, W, O, OD, G)
BWQFilter<T>      --> Constructeur chaînable, implémente IQueryable<T> / IEnumerable<T>
```

## 📋 Syntaxe BWOQ — Référence Rapide

| Opération | Syntaxe | Exemple | Description |
| --------- | ------- | ------- | ----------- |
| **Select (Q)** | `<masque>` | `Q("6")` | Projette Name (2) + City (4) = 6 |
| **Where (W)** — like | `<masque>::<valeur>` | `W("2::silva")` | Similarité sur les chaînes, insensible à la casse |
| Égalité | `<masque>::<valeur>=` | `W("32::1=")` | `=`, avec `&` = conjonction AND entre colonnes (`32::1&=`) |
| Supérieur à | `<masque>::<valeur>+` | `W("16::40+")` | `>` (en agrégation : Max) |
| Inférieur à | `<masque>::<valeur>-` | `W("16::30-")` | `<` (en agrégation : Min) |
| Supérieur ou égal | `<masque>::<valeur>=+` | `W("16::35=+")` | `>=` |
| Inférieur ou égal | `<masque>::<valeur>=-` | `W("16::35=-")` | `<=` |
| Comparaison interne | `<masque>::<valeur><[&]<suffixe>` | `W("6::10<-")` | Compare deux colonnes (masque pair) entre elles ; le `<` active ce mode, le suffixe final définit l'opérateur (`=`/`+`/`-`/`=+`/`=-`), `&` = AND entre les paires |
| **OrderBy (O)** | `<masque>` | `O("2")` | Ascendant par Name (2) |
| **OrderByDescending (OD)** | `<masque>` | `OD("64")` | Descendant par CreditLimit (64) |
| **GroupBy (G)** | `<agr>, <by>` | `G("4", "4")` | Regroupe par City (4) ; `agr` peut porter un suffixe d'agrégation |
| Count | `<masque>*` | `G("4*", "4")` | `CountResult` par groupe |
| Sum | `<masque>^` | `G("64^", "4")` | `SumOfCreditLimits` |
| Moyenne | `<masque>~` | `G("16~", "4")` | `AverageOfAges` |
| Max | `<masque>+` | `G("64+", "4")` | `MaximumOfCreditLimits` |
| Min | `<masque>-` | `G("64-", "4")` | `MinimumOfCreditLimits` |
| Navigation | `<masque>>ordinal:masque` | `Q("3>1:2")` | Projette `Credential.Logon` (agrégé à l'ordinal 1) |

---

## 📌 Exemple d'Entité et Table Binaire

Chaque attribut reçoit une puissance de 2 dans l'ordre de déclaration :

```csharp
public class Person
{
    public decimal Id { get; set; }          // 1
    public string Name { get; set; }         // 2
    public string City { get; set; }         // 4
    public string State { get; set; }        // 8
    public decimal Age { get; set; }         // 16
    public bool Active { get; set; }         // 32
    public decimal CreditLimit { get; set; } // 64
}
```

La **combinaison** (somme binaire) identifie un ensemble d'attributs :

```text
Name + City        = 2 + 4  = 6
Age + Active       = 16 + 32 = 48
toutes les colonnes = 1+2+4+8+16+32+64 = 127
```

> 💡 Les valeurs booléennes par binaire peuvent être indiquées comme `1` (true) / `0` (false)
> ou littéralement `true` / `false`.

---

## ⚡ Q — Sélection de Colonnes (Projection)

```csharp
var bwq = new BitWiseQuery<Person>(personList.AsQueryable());

// Projection de Name (2) + City (4) uniquement
var projected = bwq.Query("6", standAlone: true);

// Toutes les colonnes (127), mode filtre (chaînable)
var all = bwq.Q("127");
```

---

## 🔍 W — Filtres (Critères)

Syntaxe : `#::valeur`  →  `#` est le binaire du/des attribut(s) et `valeur` est le critère.

### Égalité — `=` (et conjonction `&`)

```csharp
// Active (32) = true  (valeur 1 + Égalité, utilisant la conjonction &)
var actifs = bwq.W("32::1&=");

// Égalité pure à 1
var actifs2 = bwq.W("32::1=");
```

> Le token `&` applique la **conjonction And** au critère. En sélectionnant plus d'un attribut
> dans le binaire (ex. : `6` = Name + City), la même valeur et comparaison sont appliquées à tous
> — sans `&` le comportement est disjonction `Or`.

### Similarité 'like' — patron pour les chaînes (insensible à la casse)

```csharp
// Name contient "Silva"
var byName = bwq.W("2::silva");

// City contient "paulo" (3 personnes de São Paulo)
var byCity = bwq.W("4::paulo");
```

### Comparateurs numériques

```csharp
var plusAgés = bwq.W("16::40+");     // Age >  40
var plusJeunes = bwq.W("16::30-");   // Age <  30
var plusAgésOuÉgaux = bwq.W("16::35=+");  // Age >= 35
var jusquÀ35 = bwq.W("16::35=-");   // Age <= 35
```

---

## ✏️ O / OD — Tri

```csharp
// Ascendant par Name (2) des actifs
var sorted = bwq.Q("127").W("32::1&=").O("2");

// Descendant par CreditLimit (64)
var sortedDesc = bwq.Q("127").W("32::1&=").OD("64");
```

---

## 🧮 G — Groupement

```csharp
// Groupement par City (4), retournant les groupes avec Key
var groups = bwq.G("4", "4");

// Groupement par City (4) des actifs uniquement
var groupsActive = bwq.Q("127").W("32::1&=").G("4", "4");
```

---

## ∑ Agrégations (suffixes postfixés)

Les agrégations opèrent sur la colonne indiquée dans le binaire, groupée par `by` :

| Suffixe | Opération | Champ généré dans le résultat |
| ------ | --------- | ----------------------------- |
| `*`    | Count     | `CountResult`                 |
| `^`    | Sum       | `SumOf<Atribute>s`            |
| `~`    | Average   | `AverageOf<Atribute>s`        |
| `+`    | Max       | `MaximumOf<Atribute>s`        |
| `-`    | Min       | `MinimumOf<Atribute>s`        |

```csharp
// Nombre d'actifs par City
var countByCity = bwq.Q("127").W("32::1&=").G("4*", "4");   // { City: CountResult }

// Somme de CreditLimit (64 / actifs) par City
var sumByCity = bwq.Q("127").W("32::1&=").G("64^", "4");    // { City: SumOfCreditLimits }

// Moyenne de Age (16 / actifs) par City
var avgByCity = bwq.Q("127").W("32::1&=").G("16~", "4");    // { City: AverageOfAges }

// Max et Min de CreditLimit par City
var maxByCity = bwq.Q("127").W("32::1&=").G("64+", "4");    // { City: MaximumOfCreditLimits }
var minByCity = bwq.Q("127").W("32::1&=").G("64-", "4");    // { City: MinimumOfCreditLimits }
```

---

## 🔗 Navigation dans les Objets Agrégés

Les attributs qui sont **des classes du même module** sont exclus de la table binaire plate et accessibles
par navigation `>position:masque`.

```csharp
public class Employee
{
    public decimal Id { get; set; }             // 1
    public string Name { get; set; }            // 2
    public decimal Age { get; set; }            // 4
    public bool Active { get; set; }            // 8

    public Credential Credential { get; set; }  // agrégé (position ordinale 1)
}

public class Credential
{
    public decimal Id { get; set; }             // 1
    public string Logon { get; set; }           // 2
    public string TokenId { get; set; }         // 4
}
```

### Navigation dans le Prédicat — `Q`

```csharp
var empBwq = new BitWiseQuery<Employee>(employeeList.AsQueryable());

// Id + Name (3) de l'Employee + Logon (2) du Credential
var proj = empBwq.Q("3>1:2", true);

// Actifs (8) + TokenId (4) du Credential
var proj2 = empBwq.Q("8>1:4", true);

// Age (4) + tous les champs du Credential (7)
var projDeep = empBwq.Q("4>1:7", true);
```

### Navigation dans le Filtre — `W`

```csharp
// Name (2) de l'Employee + Logon (2) du Credential, tous deux par like de "ana"
var byCredential = empBwq.Q("15").W("2>1:2::ana");
```

---

## 🔗 Constructeur Chaînable

Le `BWQFilter<T>` implémente `IQueryable<T>` ; il suffit de chaîner et d'énumérer :

```csharp
var result = bwq.Q("127")
                .W("32::1&=")
                .O("2")
                .ToList();            // List<Person>

foreach (var person in result)
    Console.WriteLine($"{person.Name} - {person.City}");
```

---

## 📄 Sorties JSON / CSV

```csharp
var json = bwq.Where("32::1&=", EnumSerialDataType.JSON);
var csv  = bwq.Where("32::1&=", EnumSerialDataType.CSV);
```

---

## ❓ Référence de Syntaxe

### Prédicat (sélection d'attributs)

| Token  | Signification                                        |
| ------ | ---------------------------------------------------- |
| `N`    | Combinaison binaire des attributs sélectionnés        |
| `>`    | Début de navigation vers l'objet agrégé               |
| `N`    | Position ordinale de l'agrégé                         |
| `:`    | Token pour 'obtenir' les attributs de l'agrégé        |
| `M`    | Binaire des attributs sélectionnés de l'agrégé        |
| `*`    | Count                                               |
| `^`    | Sum                                                 |
| `~`    | Average                                             |
| `+`    | Max                                                 |
| `-`    | Min                                                 |

### Critère (filtre Where)

| Token          | Exemple          | Signification                        |
| -------------- | ---------------- | ------------------------------------ |
| `#::`          | `32::1&=`        | 'où la valeur est'                   |
| `&`            | `32::1&=`        | Conjonction **And** (par défaut: `Or`) |
| `=`            | `32::1=`         | Égalité                              |
| *(default)*    | `2::carlos`      | Similarité ('like') pour les chaînes |
| `+`            | `16::40+`        | Supérieur à (en agrégation: Max)     |
| `-`            | `16::30-`        | Inférieur à (en agrégation: Min)     |
| `=+`           | `16::35=+`       | Supérieur ou égal                    |
| `=-`           | `16::35=-`       | Inférieur ou égal                    |

---

## 🔧 Méthodes Disponibles

### BitWiseQuery\<T\> (moteur de requête)

| Méthode                        | Alias              | Retour               |
| ----------------------------- | ------------------ | -------------------- |
| `Query(string)`               | `Q(string)`        | `BWQFilter<T>` (constructeur) |
| `Query(string, bool)`         | `Q(string, standAlone)` | `IQueryable` (projection) |
| `Query(string, EnumSerialDataType)` | `Q(...)`       | `string` (JSON/CSV) |
| `Where(string)`               | `W(string)`        | `IQueryable`      |
| `Where(string, bool)`         | `W(string, hasSufix)` | `BWQFilter<T>` |
| `Where(string, EnumSerialDataType)` | `W(...)`     | `string` (JSON/CSV) |
| `OrderBy(string)`             | `O(string)`        | `IQueryable`   |
| `OrderByDescending(string)`   | `OD(string)`       | `IQueryable`   |
| `GroupBy(string, string)`     | `G(by, grp)`       | `IQueryable`   |

### BWQFilter\<T\> (constructeur)

| Méthode          | Retour     |
| --------------- | ---------- |
| `W(string)`     | `BWQFilter<T>` |
| `O(string)`     | `BWQFilter<T>` |
| `OD(string)`    | `BWQFilter<T>` |
| `G(string, string)` | `IQueryable` |

---

## 🧪 Tests

Le paquet `Rochas.BWOQ.Test` couvre (**56 tests réussis / 0 échecs**) :

- Projection de colonnes (`Q(..., true)`) et navigation dans les agrégés ;
- Filtres : `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, y compris les attributs d'agrégé et comparaison interne (`<`) ;
- Tri `O` / `OD` ;
- Constructeur chaînable `Q().W().O()/OD()` ;
- Groupement `G` et agrégations `Count`, `Sum`, `Average`, `Max`, `Min` ;
- Constructeurs `IList` et `ref` ; surcharges de sérialisation `JSON` / `CSV` ;
- Héritage base→dérivé avec masques `BigInteger` (> bit 31) ;
- Helpers `BitWiseTable`, `Reflector` (clone/projection/typage) et `Serializer` (CSV) ;
- Messages d'exceptions personnalisées.

### Couverture de code (assembly Rochas.BWOQ, coverlet)

| Métrique         | Valeur |
| ---------------- | ------ |
| Lignes           | 95.19% |
| Branches         | 97.56% |

| Type                                        | Couverture |
| ------------------------------------------- | ---------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95.3% / 100% |
| Helpers: BitWiseTable, Reflector, Serializer | 100% / 87.5% / 88% |
| Exceptions personnalisées (`Invalid*Exception`) | 100% |

> L'implémentation du `Reflector` (clone/projection/typage) a été déplacée vers le paquet `Rochas.SqlWrapper` (`EntityReflector`) ; ici le `Reflector` reste uniquement comme façade de délégation.

---

## Deutsch

# README - BWOQ (Rochas.BWOQ)

**BWOQ (BitWise Object Query)** ist eine Komponente zur kompakten Zusammensetzung von Abfragen über Objektsammlungen im Speicher, unter Verwendung der **Bitwise-Notation** und **Postfix-Operatoren** (umgekehrte polnische Notation).

Jedes Attribut wird zur Laufzeit in einer **Binärtabelle** indexiert und mit der logischen Disjunktion der Indizes (Prädikate) und der Kombination von Kriterien setzen Sie **Select**, **Where**, **OrderBy / OrderByDescending** und **GroupBy** mit sehr wenig Schreibaufwand zusammen — zusätzlich zur **JSON**- oder **CSV**-Ausgabe der Ergebnisse.

---

## 📌 Installation

```bash
dotnet add package Rochas.BWOQ
```

---

## 📌 Klassennamen

```text
BitWiseQuery<T>   --> Abfragemotor (lange Methoden + Aliase Q, W, O, OD, G)
BWQFilter<T>      --> Kettbarer Builder, implementiert IQueryable<T> / IEnumerable<T>
```

## 📋 BWOQ-Syntax — Schnellreferenz

| Operation | Syntax | Beispiel | Beschreibung |
| --------- | ------ | -------- | ------------ |
| **Select (Q)** | `<Maske>` | `Q("6")` | Projiziert Name (2) + City (4) = 6 |
| **Where (W)** — like | `<Maske>::<Wert>` | `W("2::silva")` | Ähnlichkeit für Strings, ohne Groß-/Kleinschreibung |
| Gleichheit | `<Maske>::<Wert>=` | `W("32::1=")` | `=`, mit `&` = UND-Verknüpfung zwischen Spalten (`32::1&=`) |
| Größer als | `<Maske>::<Wert>+` | `W("16::40+")` | `>` (bei Aggregation: Max) |
| Kleiner als | `<Maske>::<Wert>-` | `W("16::30-")` | `<` (bei Aggregation: Min) |
| Größer oder gleich | `<Maske>::<Wert>=+` | `W("16::35=+")` | `>=` |
| Kleiner oder gleich | `<Maske>::<Wert>=-` | `W("16::35=-")` | `<=` |
| Interne Vergleiche | `<Maske>::<Wert><[&]<Suffix>` | `W("6::10<-")` | Vergleicht zwei Spalten (gerade Maske) miteinander; `<` aktiviert diesen Modus, das End-Suffix legt den Operator fest (`=`/`+`/`-`/`=+`/`=-`), `&` = AND zwischen den Paaren |
| **OrderBy (O)** | `<Maske>` | `O("2")` | Aufsteigend nach Name (2) |
| **OrderByDescending (OD)** | `<Maske>` | `OD("64")` | Absteigend nach CreditLimit (64) |
| **GroupBy (G)** | `<agr>, <by>` | `G("4", "4")` | Gruppiert nach City (4); `agr` kann Aggregationssuffix tragen |
| Count | `<Maske>*` | `G("4*", "4")` | `CountResult` pro Gruppe |
| Sum | `<Maske>^` | `G("64^", "4")` | `SumOfCreditLimits` |
| Durchschnitt | `<Maske>~` | `G("16~", "4")` | `AverageOfAges` |
| Max | `<Maske>+` | `G("64+", "4")` | `MaximumOfCreditLimits` |
| Min | `<Maske>-` | `G("64-", "4")` | `MinimumOfCreditLimits` |
| Navigation | `<Maske>>Ordinal:Maske` | `Q("3>1:2")` | Projiziert `Credential.Logon` (Aggregat an Ordinalstelle 1) |

---

## 📌 Entitäts- und Binärtabelle-Beispiel

Jedes Attribut erhält eine Potenz von 2 in der Deklarationsreihenfolge:

```csharp
public class Person
{
    public decimal Id { get; set; }          // 1
    public string Name { get; set; }         // 2
    public string City { get; set; }         // 4
    public string State { get; set; }        // 8
    public decimal Age { get; set; }         // 16
    public bool Active { get; set; }         // 32
    public decimal CreditLimit { get; set; } // 64
}
```

Die **Kombination** (binäre Summe) identifiziert eine Attributmenge:

```text
Name + City        = 2 + 4  = 6
Age + Active       = 16 + 32 = 48
alle Spalten        = 1+2+4+8+16+32+64 = 127
```

> 💡 Boolesche Werte können als `1` (true) / `0` (false)
> oder wörtlich als `true` / `false` angegeben werden.

---

## ⚡ Q — Spaltenauswahl (Projektion)

```csharp
var bwq = new BitWiseQuery<Person>(personList.AsQueryable());

// Projektion nur von Name (2) + City (4)
var projected = bwq.Query("6", standAlone: true);

// Alle Spalten (127), Filtermodus (kettbar)
var all = bwq.Q("127");
```

---

## 🔍 W — Filter (Kriterien)

Syntax: `#::wert`  →  `#` ist der Binärwert des/des Attribut(s) und `wert` ist das Kriterium.

### Gleichheit — `=` (und Konjunktion `&`)

```csharp
// Active (32) = true  (Wert 1 + Gleichheit, mit Konjunktion &)
var aktive = bwq.W("32::1&=");

// Reine Gleichheit zu 1
var aktive2 = bwq.W("32::1=");
```

> Das Token `&` wendet die **And-Konjunktion** auf das Kriterium an. Bei der Auswahl von mehr als einem Attribut
> im Binärwert (z.B.: `6` = Name + City) werden derselbe Wert und Vergleich auf alle angewendet
> — ohne `&` ist das Verhalten Disjunktion `Or`.

### Ähnlichkeit 'like' — Muster für Strings (Groß-/Kleinschreibung ignoriert)

```csharp
// Name enthält "Silva"
var byName = bwq.W("2::silva");

// City enthält "paulo" (3 Personen aus São Paulo)
var byCity = bwq.W("4::paulo");
```

### Numerische Vergleichsoperatoren

```csharp
var ältere = bwq.W("16::40+");     // Age >  40
var jüngere = bwq.W("16::30-");    // Age <  30
var ältereOderGleich = bwq.W("16::35=+");  // Age >= 35
var bis35 = bwq.W("16::35=-");     // Age <= 35
```

---

## ✏️ O / OD — Sortierung

```csharp
// Aufsteigend nach Name (2) der Aktiven
var sorted = bwq.Q("127").W("32::1&=").O("2");

// Absteigend nach CreditLimit (64)
var sortedDesc = bwq.Q("127").W("32::1&=").OD("64");
```

---

## 🧮 G — Gruppierung

```csharp
// Gruppierung nach City (4), gibt Gruppen mit Key zurück
var groups = bwq.G("4", "4");

// Gruppierung nach City (4) nur der Aktiven
var groupsActive = bwq.Q("127").W("32::1&=").G("4", "4");
```

---

## ∑ Aggregationen (Postfix-Suffixe)

Aggregationen arbeiten auf der im Binärwert angegebenen Spalte, gruppiert nach `by`:

| Suffix | Operation | Generiertes Feld im Ergebnis |
| ------ | --------- | ---------------------------- |
| `*`    | Count     | `CountResult`                |
| `^`    | Sum       | `SumOf<Atribute>s`           |
| `~`    | Average   | `AverageOf<Atribute>s`       |
| `+`    | Max       | `MaximumOf<Atribute>s`       |
| `-`    | Min       | `MinimumOf<Atribute>s`       |

```csharp
// Anzahl der Aktiven nach City
var countByCity = bwq.Q("127").W("32::1&=").G("4*", "4");   // { City: CountResult }

// Summe von CreditLimit (64 / Aktive) nach City
var sumByCity = bwq.Q("127").W("32::1&=").G("64^", "4");    // { City: SumOfCreditLimits }

// Durchschnitt von Age (16 / Aktive) nach City
var avgByCity = bwq.Q("127").W("32::1&=").G("16~", "4");    // { City: AverageOfAges }

// Max und Min von CreditLimit nach City
var maxByCity = bwq.Q("127").W("32::1&=").G("64+", "4");    // { City: MaximumOfCreditLimits }
var minByCity = bwq.Q("127").W("32::1&=").G("64-", "4");    // { City: MinimumOfCreditLimits }
```

---

## 🔗 Navigation in Aggregierten Objekten

Attribute, die **Klassen desselben Moduls** sind, werden von der flachen Binärtabelle ausgeschlossen und über
Navigation `>Position:Maske` zugegriffen.

```csharp
public class Employee
{
    public decimal Id { get; set; }             // 1
    public string Name { get; set; }            // 2
    public decimal Age { get; set; }            // 4
    public bool Active { get; set; }            // 8

    public Credential Credential { get; set; }  // Aggregat (ordinale Position 1)
}

public class Credential
{
    public decimal Id { get; set; }             // 1
    public string Logon { get; set; }           // 2
    public string TokenId { get; set; }         // 4
}
```

### Navigation im Prädikat — `Q`

```csharp
var empBwq = new BitWiseQuery<Employee>(employeeList.AsQueryable());

// Id + Name (3) des Employee + Logon (2) des Credential
var proj = empBwq.Q("3>1:2", true);

// Aktive (8) + TokenId (4) des Credential
var proj2 = empBwq.Q("8>1:4", true);

// Age (4) + alle Credential-Felder (7)
var projDeep = empBwq.Q("4>1:7", true);
```

### Navigation im Filter — `W`

```csharp
// Name (2) des Employee + Logon (2) des Credential, beide per like von "ana"
var byCredential = empBwq.Q("15").W("2>1:2::ana");
```

---

## 🔗 Kettbarer Builder

Der `BWQFilter<T>` implementiert `IQueryable<T>`; ketten Sie einfach und enumerieren:

```csharp
var result = bwq.Q("127")
                .W("32::1&=")
                .O("2")
                .ToList();            // List<Person>

foreach (var person in result)
    Console.WriteLine($"{person.Name} - {person.City}");
```

---

## 📄 JSON / CSV-Ausgaben

```csharp
var json = bwq.Where("32::1&=", EnumSerialDataType.JSON);
var csv  = bwq.Where("32::1&=", EnumSerialDataType.CSV);
```

---

## ❓ Syntaxreferenz

### Prädikat (Attributauswahl)

| Token  | Bedeutung                                          |
| ------ | -------------------------------------------------- |
| `N`    | Binärkombination der ausgewählten Attribute         |
| `>`    | Beginn der Navigation zum Aggregatobjekt            |
| `N`    | Ordinale Position des Aggregats                     |
| `:`    | Token zum 'Abrufen' der Attribute des Aggregats     |
| `M`    | Binärwert der ausgewählten Attribute des Aggregats  |
| `*`    | Count                                              |
| `^`    | Sum                                                |
| `~`    | Average                                            |
| `+`    | Max                                                |
| `-`    | Min                                                |

### Kriterium (Where-Filter)

| Token          | Beispiel         | Bedeutung                            |
| -------------- | ---------------- | ------------------------------------ |
| `#::`          | `32::1&=`        | 'wo der Wert ist'                    |
| `&`            | `32::1&=`        | **And**-Konjunktion (Standard: `Or`) |
| `=`            | `32::1=`         | Gleichheit                           |
| *(default)*    | `2::carlos`      | Ähnlichkeit ('like') für Strings     |
| `+`            | `16::40+`        | Größer als (bei Aggregation: Max)    |
| `-`            | `16::30-`        | Kleiner als (bei Aggregation: Min)   |
| `=+`           | `16::35=+`       | Größer oder gleich                   |
| `=-`           | `16::35=-`       | Kleiner oder gleich                  |

---

## 🔧 Verfügbare Methoden

### BitWiseQuery\<T\> (Abfragemotor)

| Methode                       | Alias              | Rückgabe              |
| ----------------------------- | ------------------ | --------------------- |
| `Query(string)`               | `Q(string)`        | `BWQFilter<T>` (Builder) |
| `Query(string, bool)`         | `Q(string, standAlone)` | `IQueryable` (Projektion) |
| `Query(string, EnumSerialDataType)` | `Q(...)`       | `string` (JSON/CSV) |
| `Where(string)`               | `W(string)`        | `IQueryable`      |
| `Where(string, bool)`         | `W(string, hasSufix)` | `BWQFilter<T>` |
| `Where(string, EnumSerialDataType)` | `W(...)`     | `string` (JSON/CSV) |
| `OrderBy(string)`             | `O(string)`        | `IQueryable`   |
| `OrderByDescending(string)`   | `OD(string)`       | `IQueryable`   |
| `GroupBy(string, string)`     | `G(by, grp)`       | `IQueryable`   |

### BWQFilter\<T\> (Builder)

| Methode         | Rückgabe   |
| --------------- | ---------- |
| `W(string)`     | `BWQFilter<T>` |
| `O(string)`     | `BWQFilter<T>` |
| `OD(string)`    | `BWQFilter<T>` |
| `G(string, string)` | `IQueryable` |

---

## 🧪 Tests

Das Paket `Rochas.BWOQ.Test` deckt ab (**56 Tests bestanden / 0 Fehler**):

- Spaltenprojektion (`Q(..., true)`) und Aggregatnavigation;
- Filter: `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, einschließlich Aggregatattribute und interner Vergleich (`<`);
- Sortierung `O` / `OD`;
- Kettbarer Builder `Q().W().O()/OD()`;
- Gruppierung `G` und Aggregationen `Count`, `Sum`, `Average`, `Max`, `Min`;
- `IList`- und `ref`-Konstruktoren; Überladungen der Serialisierung `JSON` / `CSV`;
- Vererbung Basis→abgeleitet mit `BigInteger`-Masken (> bit 31);
- `BitWiseTable`, `Reflector` (Klon/Projektion/Typisierung) und `Serializer` (CSV) Helfer;
- Benutzerdefinierte Ausnahmemeldungen.

### Code-Abdeckung (Assembly Rochas.BWOQ, coverlet)

| Metrik            | Wert  |
| ----------------- | ----- |
| Zeilen            | 95.19% |
| Branches          | 97.56% |

| Typ                                         | Abdeckung |
| ------------------------------------------- | --------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95.3% / 100% |
| Helfer: BitWiseTable, Reflector, Serializer | 100% / 87.5% / 88% |
| Benutzerdefinierte Ausnahmen (`Invalid*Exception`) | 100% |

> Die Implementierung des `Reflector` (Klon/Projektion/Typisierung) wurde in das Paket `Rochas.SqlWrapper` (`EntityReflector`) verschoben; hier bleibt der `Reflector` nur als Delegationsfassade.