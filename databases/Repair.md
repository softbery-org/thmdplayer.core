# Repair

## Działanie metod:
> Porównuje kolumny w dwóch schematach
> i generuje odpowiednie polecenia SQL do synchronizacji
> Obsługuje różnice w wielkości liter i typach danych
> oraz generuje ALTER TABLE dla brakujących kolumn
> i różnic w definicji kolumn

### CompareColumns:
- Wykrywa brakujące kolumny i generuje ALTER TABLE ADD COLUMN
- Porównuje typy danych, nullowalność i wartości domyślne
- Generuje ALTER TABLE MODIFY COLUMN dla różnic
- Loguje dodatkowe kolumny bez ich usuwania

### CompareIndexes:
- Porównuje indeksy po nazwie i kolumnach
- Generuje CREATE INDEX dla brakujących indeksów
- Usuwa indeksy niezgodne z definicją (oprócz PRIMARY KEY)
- Obsługuje indeksy unikalne

### __Przykład użycia:__

```csharp
// Spodziewany schemat
var expected = JArray.Parse(@"
[
  {
    'name': 'Id',
    'type': 'INT',
    'nullable': false,
    'default': null
  },
  {
    'name': 'Name',
    'type': 'VARCHAR(100)',
    'nullable': false,
    'default': null
  }
]");

// Aktualny schemat
var actual = JArray.Parse(@"
[
  {
    'name': 'id',
    'type': 'INT',
    'nullable': true,
    'default': null
  }
]");

var repairs = new List<string>();
CompareColumns(expected, actual, "Users", repairs);

// Wynik:
// ALTER TABLE Users ADD COLUMN Name VARCHAR(100) NOT NULL;
// ALTER TABLE Users MODIFY COLUMN Id INT NOT NULL;
```
## Bezpieczeństwo:
    - Unika usuwania kolumn z danymi
    - Generuje tylko operacje niskiego ryzyka 
    - Obsługuje różnice w wielkości liter
    - Formatuje wartości domyślne zgodnie ze standardami SQL