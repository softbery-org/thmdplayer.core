# Migracja aktualizacji wersji bazy danych
Mechanizm kolejności aktualizacji działa poprzez sortowanie wersji w słowniku `SortedDictionary<Version, string>`. Kluczowe elementy tego rozwiązania:

##### 1. Struktura przechowywania migracji

```sh
private readonly SortedDictionary<Version, string> _migrations;
SortedDictionary automatycznie sortuje wpisy rosnąco według klucza (Version).
```

> Klasa Version implementuje interfejs `IComparable`, co umożliwia poprawne porównywanie wersji.

##### 2. Dodawanie migracji w określonej kolejności
```sh
AddMigration(1, 0, 0, "CREATE TABLE Products...");  // wersja 1.0.0
AddMigration(1, 1, 0, "ALTER TABLE Products...");  // wersja 1.1.0
AddMigration(2, 0, 0, "CREATE TABLE Orders...");   // wersja 2.0.0
```
> Migracje są dodawane w kolejności od najstarszej do najnowszej.
> Każda migracja jest powiązana z unikalnym numerem wersji w formacie Major.Minor.Build.

##### 3. Automatyczne sortowanie i aplikowanie migracji

```sh
foreach (var migration in _migrations)
{
    if (migration.Key > currentVersion)
    {
        ApplyMigration(...);
    }
}
```

> Pętla iteruje po migracjach w kolejności posortowanej (od najniższej do najwyższej wersji).
> Migracje są aplikowane tylko jeśli ich wersja jest wyższa niż aktualna wersja bazy.

##### 4. Przykład działania kolejnościowego

|Wersja migracji|Kolejność aplikacji|
|---|---|
|1.0.0|1|
|1.1.0|2|
|1.2.0|3|
|2.0.0|4|

##### 5. Zabezpieczenia
> Transakcje: Każda migracja jest wykonywana w transakcji (atomowość operacji).
> Idempotentność: Migracje powinny być bezpieczne do wielokrotnego wykonania (np. użycie `IF NOT EXISTS`).
> Blokada wersji: Tabela `VersionHistory` zapobiega równoczesnym aktualizacjom.

##### 6. Jak dodawać nowe migracje?
```sh
private void InitializeMigrations()
{
    AddMigration(1, 2, 0, "UPDATE Products SET ...");  // Nowa migracja
    AddMigration(1, 3, 0, "ALTER TABLE Orders ...");    // Kolejna migracja
}
```
> Nowe migracje dodawane są na końcu listy, z wyższym numerem wersji.
> Słownik `SortedDictionary` automatycznie zachowa właściwą kolejność.

> Dlaczego to działa?
> Sortowanie leksykograficzne: Wersja 1.10.0 jest traktowana jako większa niż 1.2.0 (poprawne porównywanie numerów).

> Elastyczność: Możliwość dodawania migracji dla "łat" (1.1.0, 1.2.0) i "głównych wersji" (2.0.0, 3.0.0).

##### Przykład awarii kolejności
Jeśli dodasz migracje w złej kolejności:

```sh
AddMigration(2, 0, 0, "...");  // wersja 2.0.0
AddMigration(1, 0, 0, "...");  // wersja 1.0.0
SortedDictionary i tak posortuje je jako 1.0.0 → 2.0.0.
```

> Ale! Lepiej dodawać migracje ręcznie w kolejności, aby uniknąć niejasności.

##### Podsumowanie
Metoda `AddMigration` gwarantuje kolejność aktualizacji poprzez:
- Powiązanie migracji z numerem wersji.
- Automatyczne sortowanie według rosnących wersji.
- Aplikowanie tylko niezbędnych migracji w odpowiedniej kolejności.

Reposytorium do pobrania na [github.com][git-url-repo]

[//]: #Links
[git-url-repo]: <https://github.com/softbery-org/>

# ZMIANY:
## Struktura katalogów i plików:

📁 Project/
├─ 📁 Migrations/
│  ├─ 1.0.0.sql
│  ├─ 1.1.0.sql
│  └─ 2.0.0.sql
├─ DatabaseUpdater.cs
└─ ... 

### 3. Przykładowy plik migracji 1.0.0.sql:
```sql
CREATE TABLE IF NOT EXISTS Products (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL
) ENGINE=InnoDB;
```

### 4. Kluczowe zmiany:
Automatyczne wykrywanie migracji z katalogu na podstawie nazw plików (np. 1.0.0.sql)
Obsługa ręcznego dodawania migracji z niestandardowymi nazwami plików

Walidacja plików:
Sprawdzanie istnienia katalogu migracji

Wykrywanie duplikatów wersji:
Obsługa błędów parsowania wersji

### 5. Konfiguracja:
```csharp
var updater = new DatabaseUpdater(
    connectionString: "Server=localhost;Database=ExampleDB;Uid=root;Pwd=;",
    migrationsDirectory: "Database/Migrations" // opcjonalna ścieżka
);
updater.UpdateDatabase();
```
### 6. Zalety rozwiązania:
- Separacja kodu i skryptów SQL
- Automatyczne sortowanie migracji po wersjach
- Elastyczność w nazewnictwie plików
- Bezpieczeństwo: Transakcje i rollback w przypadku błędów
- Łatwe zarządzanie historią zmian

### 7. Rozszerzenia (opcjonalnie):
Dodaj metodę `ValidateMigrationHashes()` dla integralności plików
Zaimplementuj mechanizm cofania migracji (downgrade)
Dodaj logowanie do pliku dla audytu migracji

Ta implementacja zapewnia profesjonalne zarządzanie migracjami z zachowaniem dobrych praktyk DevOps.

# Migracja aktualizacji wersji bazy danych
Ścisła kontrola zależności między migracjami
Automatyczna weryfikacja poprzedniej wersji
Jawna deklaracja wymaganej wersji bazowej
Bezpieczeństwo przed aplikacją migracji w złej kolejności
Możliwość rozwoju nieliniowych ścieżek migracji