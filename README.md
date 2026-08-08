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

O pacote `Rochas.BWOQ.Test` cobre (18 testes aprovados / 0 falhas):

- Projeção de colunas (`Q(..., true)`) e navegação em agregados;
- Filtros: `=`, `&` (AND), like, `+`, `-`, `=+`, `=-`, inclusive em atributos de agregado;
- Ordenação `O` / `OD`;
- Builder encadeado `Q().W().O()/OD()`;
- Agrupamento `G` e agregações `Count`, `Sum`, `Max`.