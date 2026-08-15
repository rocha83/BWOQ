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
BwoqQuery<T>      --> Façade (v1.6.0) com 3 modos de execução: LINQ / ANSI SQL / DapperRepository
```

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

O pacote `Rochas.BWOQ.Test` cobre (**70 testes aprovados / 0 falhas**):

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
| Linhas            | 95,87% |
| Branches          | 88,98% |

| Tipo                                        | Cobertura |
| ------------------------------------------- | --------- |
| BitWiseQuery&lt;T&gt; / BWQFilter&lt;T&gt;  | 95,0% / 100% |
| Helpers: BitWiseTable, Reflector, Serializer | 100% / 94,1% / 98,4% |
| Exceções customizadas (`Invalid*Exception`)  | 100% |

---

## 🚀 Façade `BwoqQuery<T>` — 3 Modos de Execução (v1.6.0)

O `BwoqQuery<T>` é o façade de integração com **Rochas.Data.Specification**
(sem dependência da ORM): a mesma expressão BWOQ é traduzida para **LINQ**, **SQL ANSI
multi-dialeto** ou um **comando do GenericRepository** (via reflexão).

```csharp
using Rochas.BWOQ.Data;
using Rochas.Data.Specification.Enums;
```

Composição imutável por fluência — todos os métodos validam a sintaxe no momento da chamada:

```csharp
var query = BwoqQuery<Person>.Create()
    .Select("6")                    // Name (2) + City (4)
    .Where("32::1&=")               // Active = true
    .Where("16::35=+")              // Age >= 35
    .OrderBy("2");                  // Name ASC
```

### Modo 1 — LINQ (fonte do caller)

`Apply(IQueryable<T>)` devolve um `IQueryable` em que `Select` vira projeção dinâmica e
`Where`/`OrderBy`/`GroupBy` filtram e agregam a fonte:

```csharp
var result = query.Apply(personList.AsQueryable()).ToList();
```

> Quando `Select` é omitido, o façade projeta todas as colunas e devolve entidades tipadas
> (`IQueryable<Person>`), permitindo `ToList()`/`foreach` diretos.

### Modo 2 — SQL ANSI (somente string, nunca executa)

`ToSql(DatabaseEngine)` devolve a string do comando para o dialeto escolhido
(`MySQL`, `SQLServer`, `PostgreSQL`, `SQLite`):

```csharp
var sql = query.ToSql(DatabaseEngine.SQLServer);
// SELECT Name, City FROM Person WHERE (Active = 1) AND (Age >= 35) ORDER BY Name ASC

var sqlPostgres = BwoqQuery<SqlPeople>.Create()
    .Select("1")
    .ToSql(DatabaseEngine.PostgreSQL);
// SELECT "person_id" FROM "app"."people"
```

Identificadores são delimitados com aspas no PostgreSQL (incluindo `Schema.Table`).
`[Table]`/`[Column]` do `System.ComponentModel.DataAnnotations` são respeitados.

### Modo 3 — GenericRepository (reflexão em entidade-filtro)

`ToRepositoryQuery()` materializa a expressão como um comando do repositório sem executar:
entidade-filtro tipada `T` por reflexão, `Search` para colunas `[Filterable]`,
`[RangeFilter]` para limites `>=`/`<=`, `loadComposition` para navegação e builders de
ordenação/agrupamento com `DataAggregationType`:

```csharp
public class PersonFilter
{
    public decimal Id { get; set; }

    [Filterable]
    public string Name { get; set; }

    [RangeFilter(LinkedRangeProperty = "AgeUntil")]
    public decimal AgeFrom { get; set; }

    public decimal AgeUntil { get; set; }
    public string State { get; set; }
}

var repoQuery = BwoqQuery<PersonFilter>.Create()
    .Where("2::silva")        // Search: Name [Filterable]
    .Where("4::35=+")         // RangeFilter: AgeFrom = 35 (>=)
    .Where("8::65=-")         // RangeFilter: AgeUntil = 65 (<=)
    .OrderBy("64")
    .ToRepositoryQuery();

var builder = repoQuery.Build(repository);            // IQueryBuilder<T>
var paged   = repoQuery.Build(repository, 1, 50);     // IQueryPaginatedBuilder<T>
var sync    = repoQuery.BuildSync(repository);        // IQuerySyncBuilder<T>
```

> A execução permanece com o caller (await/ToList). Sem `[Filterable]`/`[RangeFilter]`,
> o façade lança `BwoqCapabilityException` orientando a usar SQL ou LINQ.

### Composição (loadComposition) e Disjunção (OR) — suportadas

O façade explora a API pública da Specification:

- **Composição**: projeção com navegação (`Select("3>1:2")`) liga `loadComposition = true`
  (`[RelatedEntity]`) para carregar agregados por JOIN. Assim funciona:

```csharp
var repoQuery = BwoqQuery<Employee>.Create()
    .Select("3>1:2")          // Id + Name do Employee + Logon do Credential
    .Where("8::1&=")          // Active = true (Employee)
    .ToRepositoryQuery();

Assert.True(repoQuery.LoadComposition);        // eager loading via [RelatedEntity]
// Execute: repoQuery.Build(repository).ToList() → Employee com Credential populado
```

- **Disjunção (OR)**: o GenericRepository publica `filterConjunction` — quando **toda** a
  expressão for disjuntiva (critérios sem `&`), o façade mapeia `filterConjunction = false`
  e a ORM combina as condições com `OR`:

```csharp
var repoQuery = BwoqQuery<PersonFilter>.Create()
    .Where("2::silva")        // Name like "silva" OR
    .Where("32::1=")          // Active = true   (OR)
    .ToRepositoryQuery();

Assert.False(repoQuery.FilterConjunction);     // OR
```

> Busca legada `Search(criteria, ...)` existe como otimização para um único critério like
> sobre coluna `[Filterable]`. `BulkSearch(Object[] criterias, ...)` segue disponível na
> ORM para disjunções por critérios independentes.

### Limitações por design (BwoqCapabilityException)

| Situação                              | Motivo                                     | Alternativa                     |
| ------------------------------------- | ------------------------------------------ | ------------------------------- |
| Filtro via navegação (`Where("2>1:2::...")`) | `loadComposition` carrega composição para leitura; filtrar por coluna do agregado exigiria JOIN explícito ([RelationalColumn] privada) | SQL (`ToSql`) ou LINQ (`Apply`) |
| Mistura AND + OR na mesma expressão   | a ORM aplica um único `filterConjunction` a todas as condições | use só `&` ou só disjunção |
| Comparadores estritos `>`/`<`         | ORM pública expõe só `>=`/`<=` via `[RangeFilter]` | SQL (`ToSql`) ou LINQ (`Apply`) |
| Igualdade exata `=` em string não-chave | ORM busca por semelhança (LIKE)           | SQL (`ToSql`) ou LINQ (`Apply`) |
| Booleano `false` / valor default (0, empty) | ORM ignora filtro com valor vazio          | SQL (`ToSql`) ou LINQ (`Apply`) |

O token `>` permanece **exclusivamente navegação em composição** (nunca comparação);
comparadores são `+` (maior), `-` (menor), `=+` (maior/igual), `=-` (menor/igual) e `=`
(igualdade).
