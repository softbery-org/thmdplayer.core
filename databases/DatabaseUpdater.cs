// Version: 1.0.0.470
using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ThmdPlayer.Core.databases.exceptions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace ThmdPlayer.Core.databases;

/// <summary>
/// DatabaseUpdater is a class responsible for managing database migrations.
/// </summary>
/// 
/// How to use:
/// 
/// var updater = new DatabaseUpdater(
/// connectionString: "Server=localhost;Database=ExampleDB;Uid=root;Pwd=;",
/// migrationsDirectory: "Database/Migrations",
/// rollbackDirectory: "Database/Rollbacks",
/// logDirectory: "AppLogs"
/// );
///
/// // Upgrade to the latest version
/// try
/// {
///     updater.UpdateDatabase();
/// }
/// catch (VersionMismatchException ex)
/// {
///     Console.WriteLine($"Problem z migracją: {ex.Message}");
///     Console.WriteLine("Wykonaj następujące kroki:");
///     Console.WriteLine($"1. Przywróć bazę do wersji {ex.Expected}");
///     Console.WriteLine($"2. Zastosuj brakujące migracje");
///     Console.WriteLine($"3. Uruchom aktualizację ponownie");
/// }
///
/// // Check integrity of migration hashes
/// if (!updater.ValidateMigrationHashes())
/// {
///    Console.WriteLine("Wykryto zmiany w migracjach!");
///}
///
/// Back to version 1.0.0
///updater.DowngradeDatabase(new Version(1, 0, 0));
public class DatabaseUpdater
{
    private readonly string _connectionString;
    private string _migrationsDir;
    private readonly string _rollbackDir;
    private readonly string _logFilePath;
    private List<Version> _sortedVersions;
    private readonly SortedDictionary<Version, MigrationScript> _migrations;
    private readonly string _repairDir;
    private readonly Dictionary<Version, string> _repairScripts = new Dictionary<Version, string>();
    private readonly string _backupDir;
    private readonly string _schemaDir;
    private readonly string _validationDir;
    private readonly string _mysqldumpPath;

    /// <summary>
    /// List of all migration scripts.
    /// </summary>
    public List<Version> SortedVersions => _sortedVersions;
    /*public SortedDictionary<Version, MigrationScript> Migrations
    {
        get
        {
            return _migrations;
        }
    }*/
    /// <summary>
    /// Directory where migration scripts are stored.
    /// </summary>
    public string MigrationsDir => _migrationsDir;

    /// <summary>
    /// Directory where rollback scripts are stored.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="migrationsDirectory"></param>
    /// <param name="rollbackDirectory"></param>
    /// <param name="repairDirectory"></param>
    /// <param name="logDirectory"></param>
    public DatabaseUpdater(string connectionString,
                           string migrationsDirectory = "Migrations",
                           string rollbackDirectory = "Rollbacks",
                           string repairDirectory = "Repairs",
                           string backupDirectory = "Backups",
                           string schemaDirectory = "Schemas",
                           string validationDirectory = "Validations",
                           string mysqldumpPath = "/usr/bin/mysqldump",
                           string logDirectory =  "Logs")
    {
        _connectionString = connectionString;
        _migrationsDir = migrationsDirectory;
        _rollbackDir = rollbackDirectory;
        _logFilePath = Path.Combine(logDirectory, $"migration_log_{DateTime.Now:yyyyMMddHHmmss}.log");
        _migrations = new SortedDictionary<Version, MigrationScript>();
        _repairDir = repairDirectory;
        _backupDir = backupDirectory;
        _schemaDir = schemaDirectory;
        _validationDir = validationDirectory;
        _mysqldumpPath = mysqldumpPath;

        Directory.CreateDirectory(_backupDir);
        Directory.CreateDirectory(_schemaDir);
        Directory.CreateDirectory(_validationDir);

        LoadRepairScripts();

        InitializeLogger();
        LoadMigrations();
    }

    public void SmartRepair()
    {
        var backupFile = CreateBackup();
        try
        {
            var repairScript = GenerateSchemaRepair();
            ApplyRepairScript(repairScript);
            ValidateDataConsistency();
        }
        catch (Exception ex)
        {
            RestoreBackup(backupFile);
            throw new Exception("Naprawa nieudana. Przywrócono backup", ex);
        }
        finally
        {
            CleanOldBackups(); // Nowa metoda do czyszczenia starych backupów
        }
    }

    #region Backup/Restore
    private string CreateBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var backupFile = Path.Combine(_backupDir, $"backup_{timestamp}.sql");

        var connectionBuilder = new MySqlConnectionStringBuilder(_connectionString);

        var args = $"""
            -u {connectionBuilder.UserID} 
            -p{connectionBuilder.Password} 
            -_h {connectionBuilder.Server} 
            {connectionBuilder.Database} 
            --result-file={backupFile} 
            --no-tablespaces
            --skip-comments
            --skip-add-drop-table
            """;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _mysqldumpPath,
                Arguments = args.Replace("\n", " "),
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Błąd backupu: {process.StandardError.ReadToEnd()}");

        return backupFile;
    }

    private void RestoreBackup(string backupFile)
    {
        var connectionBuilder = new MySqlConnectionStringBuilder(_connectionString);

        var args = $"""
            -u {connectionBuilder.UserID} 
            -p{connectionBuilder.Password} 
            -_h {connectionBuilder.Server} 
            {connectionBuilder.Database} < {backupFile}
            """;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{args}\"",
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Błąd przywracania: {process.StandardError.ReadToEnd()}");
    }

    private void CleanOldBackups()
    {
        var backupFiles = Directory.GetFiles(_backupDir, "backup_*.sql")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Skip(5) // Zachowaj ostatnie 5 backupów
            .ToList();

        foreach (var file in backupFiles)
        {
            file.Delete();
            Log($"Usunięto stary backup: {file.Name}");
        }
    }
    #endregion

    #region Schema Comparison
    private string GenerateSchemaRepair()
    {
        var connection = new MySqlConnection(_connectionString);
        var currentVersion = GetCurrentVersion(connection);
        var expectedSchema = LoadSchemaSnapshot(currentVersion);
        var actualSchema = GetCurrentSchema();
        var repairScript = new StringBuilder();

        if (repairScript.Length == 0)
        {
            Log("Brak różnic w schemacie - nie generowano skryptu naprawczego");
        }
        else
        {
            repairScript.Insert(0, "START TRANSACTION;\n");
            repairScript.AppendLine("\nCOMMIT;");
        }

        return repairScript.ToString();

        return CompareSchemas(expectedSchema, actualSchema);
    }

    private JObject LoadSchemaSnapshot(Version version)
    {
        var schemaFile = Path.Combine(_schemaDir, $"{version}.json");
        return JObject.Parse(File.ReadAllText(schemaFile));
    }

    private JObject GetCurrentSchema()
    {
        var schema = new JObject();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            // Pobierz tabele
            var tables = connection.GetSchema("Tables");
            foreach (DataRow table in tables.Rows)
            {
                var tableName = table["TABLE_NAME"].ToString();
                var columns = GetColumns(connection, tableName);
                var indexes = GetIndexes(connection, tableName);

                schema[tableName] = new JObject
                {
                    ["columns"] = columns,
                    ["indexes"] = indexes
                };
            }
        }

        return schema;
    }

    private JArray GetColumns(MySqlConnection connection, string tableName)
    {
        var columns = new JArray();
        var command = $"SHOW COLUMNS FROM {tableName}";

        using (var cmd = new MySqlCommand(command, connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                columns.Add(new JObject
                {
                    ["name"] = reader["Field"].ToString(),
                    ["type"] = reader["Type"].ToString(),
                    ["nullable"] = reader["Null"].ToString() == "YES",
                    ["default"] = reader["Default"].ToString()
                });
            }
        }
        return columns;
    }

    private JArray GetIndexes(MySqlConnection connection, string tableName)
    {
        var indexes = new JArray();
        var command = $"SHOW INDEX FROM {tableName}";

        using (var cmd = new MySqlCommand(command, connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                indexes.Add(new JObject
                {
                    ["name"] = reader["Key_name"].ToString(),
                    ["unique"] = reader["Non_unique"].ToString() == "0",
                    ["columns"] = reader["Column_name"].ToString()
                });
            }
        }
        return indexes;
    }

    private string CompareSchemas(JObject expected, JObject actual)
    {
        var repairScript = new List<string>();

        // Porównaj tabele
        foreach (var expectedTable in expected.Properties())
        {
            if (!actual.ContainsKey(expectedTable.Name))
            {
                repairScript.Add($"CREATE TABLE {expectedTable.Name} (...);");
                continue;
            }

            var actualTable = actual[expectedTable.Name] as JObject;
            CompareColumns(expectedTable.Value["columns"] as JArray,
                          actualTable["columns"] as JArray,
                          expectedTable.Name,
                          repairScript);
            CompareIndexes(expectedTable.Value["indexes"] as JArray,
                           actualTable["indexes"] as JArray,
                           expectedTable.Name,
                           repairScript);
        }

        return string.Join("\n", repairScript);
    }

    private void CompareColumns(JArray expectedColumns, JArray actualColumns, string tableName, List<string> repairScript)
    {
        var expectedDict = expectedColumns.ToDictionary(c => c["name"].ToString(), StringComparer.OrdinalIgnoreCase);
        var actualDict = actualColumns.ToDictionary(c => c["name"].ToString(), StringComparer.OrdinalIgnoreCase);

        // Sprawdź brakujące kolumny i różnice w definicjach
        foreach (JObject expectedCol in expectedColumns)
        {
            var colName = expectedCol["name"].ToString();
            var actualCol = actualDict.ContainsKey(colName) ? actualDict[colName] : null;

            if (actualCol == null)
            {
                repairScript.Add($"ALTER TABLE {tableName} ADD COLUMN {GenerateColumnDefinition(expectedCol)};");
            }
            else if (!ColumnDefinitionsEqual(expectedCol, actualCol))
            {
                repairScript.Add($"ALTER TABLE {tableName} MODIFY COLUMN {GenerateColumnDefinition(expectedCol)};");
            }
        }

        // Wykryj dodatkowe kolumny (tylko logowanie)
        foreach (JObject actualCol in actualColumns)
        {
            var colName = actualCol["name"].ToString();
            if (!expectedDict.ContainsKey(colName))
            {
                Log($"Uwaga: Dodatkowa kolumna {colName} w tabeli {tableName}", true);
            }
        }
    }

    private void CompareIndexes(JArray expectedIndexes, JArray actualIndexes, string tableName, List<string> repairScript)
    {
        var expectedDict = expectedIndexes.ToDictionary(
            i => $"{i["name"]}_{string.Join(",", i["columns"])}",
            StringComparer.OrdinalIgnoreCase
        );

        var actualDict = actualIndexes.ToDictionary(
            i => $"{i["name"]}_{string.Join(",", i["columns"])}",
            StringComparer.OrdinalIgnoreCase
        );

        // Dodaj brakujące indeksy
        foreach (JObject expectedIdx in expectedIndexes)
        {
            var key = $"{expectedIdx["name"]}_{string.Join(",", expectedIdx["columns"])}";
            if (!actualDict.ContainsKey(key))
            {
                repairScript.Add(GenerateCreateIndexCommand(tableName, expectedIdx));
            }
        }

        // Usuń niezgodne indeksy
        foreach (JObject actualIdx in actualIndexes)
        {
            var key = $"{actualIdx["name"]}_{string.Join(",", actualIdx["columns"])}";
            if (!expectedDict.ContainsKey(key) && actualIdx["name"].ToString() != "PRIMARY")
            {
                repairScript.Add($"DROP INDEX {actualIdx["name"]} ON {tableName};");
            }
        }
    }
    #endregion

    #region Data Validation
    private void ValidateDataConsistency()
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            var currentVersion = GetCurrentVersion(connection).ToString();
            var validationFiles = Directory.GetFiles(_validationDir, $"{currentVersion}_*.sql");

            connection.Open();

            foreach (var file in validationFiles)
            {
                var validationQuery = File.ReadAllText(file);

                using (var cmd = new MySqlCommand(validationQuery, connection))
                {
                    var result = cmd.ExecuteScalar();

                    if (Convert.ToInt32(result) != 0)
                        throw new Exception($"Niepowodzenie walidacji: {Path.GetFileName(file)}");
                }
            }
        }
    }
    #endregion

    #region Repair Scripts

    private void ApplyRepairScript(string repairScript)
    {
        if (string.IsNullOrWhiteSpace(repairScript))
        {
            Log("Brak komend naprawczych do wykonania");
            return;
        }

        Log($"Rozpoczynanie aplikowania skryptu naprawczego ({repairScript.Length} znaków)");

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var commands = SplitScript(repairScript);

                    foreach (var cmdText in commands)
                    {
                        if (IsDestructiveOperation(cmdText))
                        {
                            throw new InvalidOperationException(
                                $"Skrypt naprawczy zawiera destruktywną operację: {cmdText.Truncate(100)}");
                        }

                        ExecuteCommand(connection, cmdText, transaction);
                    }

                    transaction.Commit();
                    Log("Pomyślnie zastosowano skrypt naprawczy");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log($"Niepowodzenie aplikowania skryptu naprawczego: {ex.Message}", true);
                    throw new DatabaseOperationException("Błąd wykonania skryptu naprawczego", ex);
                }
            }
        }
    }

    private void LoadRepairScripts()
    {
        if (!Directory.Exists(_repairDir)) return;

        foreach (var file in Directory.GetFiles(_repairDir, "*.sql"))
        {
            var versionMatch = Regex.Match(Path.GetFileName(file), @"(\d+\.\d+\.\d+)");
            if (versionMatch.Success && Version.TryParse(versionMatch.Groups[1].Value, out var version))
            {
                _repairScripts[version] = File.ReadAllText(file);
            }
        }
    }

    public void RepairDatabase()
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            EnsureRepairHistoryExists(connection);

            var currentVersion = GetCurrentVersion(connection);
            var appliedMigrations = GetAppliedMigrations(connection);

            foreach (var version in appliedMigrations)
            {
                if (_repairScripts.TryGetValue(version, out var script))
                {
                    ApplyRepair(connection, version, script);
                }
            }
        }
    }

    private void EnsureRepairHistoryExists(MySqlConnection connection)
    {
        var createTable = @"
            CREATE TABLE IF NOT EXISTS RepairHistory (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                VersionNumber VARCHAR(20) NOT NULL,
                RepairDate DATETIME NOT NULL,
                RepairHash VARCHAR(64) NOT NULL,
                CONSTRAINT UC_Repair UNIQUE (VersionNumber, RepairHash)
            ) ENGINE=InnoDB;";

        ExecuteScript(connection, createTable);
    }

    private void ApplyRepair(MySqlConnection connection, Version version, string script)
    {
        var scriptHash = ComputeHash(script);

        if (IsRepairApplied(connection, version, scriptHash)) return;

        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                var commands = SplitScript(script);

                foreach (var cmdText in commands)
                {
                    if (IsDestructiveOperation(cmdText))
                    {
                        throw new InvalidOperationException(
                            $"Naprawa dla wersji {version} zawiera destruktywną operację: {cmdText}");
                    }

                    ExecuteCommand(connection, cmdText, transaction);
                }

                LogRepair(connection, transaction, version, scriptHash);
                transaction.Commit();
                Log($"Zastosowano naprawę dla wersji {version}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log($"Błąd naprawy wersji {version}: {ex}", isError: true);
                throw;
            }
        }
    }

    private string ComputeHash(string input)
    {
        using (var sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }

    private List<string> SplitScript(string script)
    {
        var commands = new List<string>();
        var pattern = @"(?<!\\)(?:\\\\)*;"; // Ignoruj średniki w stringach i komentarzach
        var matches = Regex.Split(script, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        foreach (var match in matches)
        {
            var cmd = match.Trim();
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                commands.Add(cmd);
            }
        }
        return commands;
    }

    private void ExecuteCommand(MySqlConnection connection, string commandText, MySqlTransaction transaction)
    {
        try
        {
            using (var cmd = new MySqlCommand(commandText, connection, transaction))
            {
                cmd.CommandTimeout = 300; // 5 minut timeout
                cmd.ExecuteNonQuery();
            }
        }
        catch (MySqlException ex)
        {
            Log($"Błąd wykonania komendy: {commandText}\nKod błędu: {ex.Number}\n{ex.Message}", true);
            throw new DatabaseOperationException("Błąd wykonania komendy SQL", ex);
        }
    }

    private bool IsDestructiveOperation(string command)
    {
        var destructivePatterns = new[]
        {
        @"\bDROP\b",
        @"\bTRUNCATE\b",
        @"\bDELETE FROM\b",
        @"\bALTER\s+TABLE.*\bDROP\b"
    };

        return destructivePatterns.Any(p =>
            Regex.IsMatch(command, p, RegexOptions.IgnoreCase));
    }

    private bool IsRepairApplied(MySqlConnection connection, Version version, string hash)
    {
        var cmd = @"
            SELECT 1 
            FROM RepairHistory 
            WHERE VersionNumber = @version 
            AND RepairHash = @hash 
            LIMIT 1";

        using (var query = new MySqlCommand(cmd, connection))
        {
            query.Parameters.AddWithValue("@version", version.ToString());
            query.Parameters.AddWithValue("@hash", hash);
            return query.ExecuteScalar() != null;
        }
    }

    private void LogRepair(MySqlConnection connection, MySqlTransaction transaction, Version version, string hash)
    {
        var scriptHash = ComputeHash(hash);

        var cmd = @"
            INSERT INTO RepairHistory (VersionNumber, RepairDate, RepairHash)
            VALUES (@version, @date, @hash)";

        using (var query = new MySqlCommand(cmd, connection, transaction))
        {
            query.Parameters.AddWithValue("@version", version.ToString());
            query.Parameters.AddWithValue("@date", DateTime.Now);
            query.Parameters.AddWithValue("@hash", scriptHash);
            query.ExecuteNonQuery();
        }
    }

    private List<Version> GetAppliedMigrations(MySqlConnection connection)
    {
        var cmd = @"
            SELECT VersionNumber 
            FROM VersionHistory 
            WHERE ActionType = 'UP'
            ORDER BY DateApplied";

        var versions = new List<Version>();

        using (var query = new MySqlCommand(cmd, connection))
        using (var reader = query.ExecuteReader())
        {
            while (reader.Read())
            {
                if (Version.TryParse(reader.GetString(0), out var version))
                {
                    versions.Add(version);
                }
            }
        }
        return versions;
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Sets the directory for migration scripts.
    /// </summary>
    /// <param name="directory"></param>
    public void SetMigrationsDirectory(string directory)
    {
        _migrationsDir = directory;
        LoadMigrations();
    }

    private void InitializeLogger()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
        Log($"Initialized database updater. Target database: {_connectionString}");
    }

    private void LoadMigration(string directory)
    {
        var version = new Version(Path.GetFileName(directory));
        var meta = JsonConvert.DeserializeObject<MigrationMeta>(
            File.ReadAllText(Path.Combine(directory, "meta.json")));

        var upScript = File.ReadAllText(Path.Combine(directory, "up.sql"));
        var downScript = File.ReadAllText(Path.Combine(directory, "down.sql"));

        _migrations.Add(version, new MigrationScript(
            upScript,
            downScript,
            new Version(meta.BaseVersion))
        );
    }

    private void LoadMigrations()
    {
        try
        {
            ValidateDirectoryStructure();
            LoadMigrationFiles();
            Log($"Loaded {_migrations.Count} migrations");
        }
        catch (Exception ex)
        {
            Log($"Error loading migrations: {ex}", isError: true);
            throw;
        }
        
        // Generate sorted versions list
        _sortedVersions = _migrations.Keys.OrderBy(v => v).ToList();
    }
    #endregion

    #region Core Functionality
    /// <summary>
    /// Updates the database to the latest version.
    /// </summary>
    public void UpdateDatabase()
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            EnsureVersionHistoryExists(connection);

            Version currentVersion = GetCurrentVersion(connection);

            foreach (var version in _sortedVersions)
            {
                if (version > currentVersion)
                {
                    ValidatePreviousVersion(connection, version);
                    ApplyMigration(connection, version, _migrations[version].UpScript);
                }
            }
        }
    }

    private void ValidatePreviousVersion(MySqlConnection connection, Version targetVersion)
    {
        var expectedVersion = _migrations[targetVersion].ExpectedBaseVersion;
        var currentVersion = GetCurrentVersion(connection);

        if (currentVersion != expectedVersion)
        {
            Log($"Błąd zgodności wersji dla {targetVersion}", isError: true);
            throw new VersionMismatchException(expectedVersion, currentVersion);
        }
    }

    /// <summary>
    /// Downgrades the database to a specific version.
    /// </summary>
    /// <param name="targetVersion"></param>
    public void DowngradeDatabase(Version targetVersion)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            EnsureVersionHistoryExists(connection);

            var currentVersion = GetCurrentVersion(connection);
            var migrationsToRollback = _migrations
                .Where(m => m.Key > targetVersion && m.Key <= currentVersion)
                .Reverse();

            foreach (var migration in migrationsToRollback)
            {
                RollbackMigration(connection, migration.Key, migration.Value.DownScript);
            }
        }
    }

    /// <summary>
    /// Validates the migration hashes in the database against the current files.
    /// </summary>
    /// <returns></returns>
    public bool ValidateMigrationHashes()
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            var command = new MySqlCommand(
                "SELECT VersionNumber, MigrationHash FROM VersionHistory WHERE ActionType = 'UP'",
                connection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var version = new Version(reader.GetString(0));
                    var storedHash = reader.GetString(1);

                    if (!_migrations.TryGetValue(version, out var migration))
                    {
                        Log($"Missing migration for version {version}", isError: true);
                        return false;
                    }

                    var currentHash = ComputeFileHash(migration.UpScriptPath);
                    if (currentHash != storedHash)
                    {
                        Log($"Hash mismatch for version {version}", isError: true);
                        return false;
                    }
                }
            }
        }
        return true;
    }
    #endregion

    #region Migration Processing
    private void ApplyMigration(MySqlConnection connection, Version version, string script)
    {
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                ExecuteScript(connection, script, transaction);
                LogMigration(connection, transaction, version, "UP");
                transaction.Commit();
                Log($"Applied migration {version}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log($"Failed to apply migration {version}: {ex}", isError: true);
                throw;
            }
        }
    }

    private void RollbackMigration(MySqlConnection connection, Version version, string script)
    {
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                ExecuteScript(connection, script, transaction);
                LogMigration(connection, transaction, version, "DOWN");
                transaction.Commit();
                Log($"Rolled back migration {version}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log($"Failed to rollback migration {version}: {ex}", isError: true);
                throw;
            }
        }
    }
    #endregion

    #region Helpers
    private class MigrationScript
    {
        public Version ExpectedBaseVersion { get; set; }
        public string UpScript { get; }
        public string DownScript { get; }
        public string UpScriptPath { get; }
        public string DownScriptPath { get; }

        /// <summary>
        /// Constructor for MigrationScript.
        /// </summary>
        /// <param name="upScript"></param>
        /// <param name="downScript"></param>
        /// <param name="upPath"></param>
        /// <param name="downPath"></param>
        public MigrationScript(string upScript, string downScript, string upPath, string downPath)
        {
            UpScript = upScript;
            DownScript = downScript;
            UpScriptPath = upPath;
            DownScriptPath = downPath;
        }

        public MigrationScript(string upScript, string downScript, Version baseVersion)
        {
            UpScript = upScript;
            DownScript = downScript;
            ExpectedBaseVersion = baseVersion;
        }
    }

    private void ValidateDirectoryStructure()
    {
        if (!Directory.Exists(_migrationsDir))
            Console.WriteLine(new DirectoryNotFoundException($"Migrations directory not found: {_migrationsDir}"));

        if (!Directory.Exists(_rollbackDir))
            Console.WriteLine(new DirectoryNotFoundException($"Rollbacks directory not found: {_rollbackDir}"));
    }

    private void LoadMigrationFiles()
    {
        var migrationFiles = Directory.GetFiles(_migrationsDir, "*.sql")
            .Select(f => new {
                Path = f,
                Version = GetVersionFromFileName(f)
            })
            .Where(f => f.Version != null);

        foreach (var file in migrationFiles)
        {
            var rollbackPath = Path.Combine(_rollbackDir, $"{file.Version}_rollback.sql");

            if (!File.Exists(rollbackPath))
                throw new FileNotFoundException($"Rollback script not found for version {file.Version}");

            var upScript = File.ReadAllText(file.Path);
            var downScript = File.ReadAllText(rollbackPath);

            _migrations.Add(file.Version, new MigrationScript(
                upScript, downScript, file.Path, rollbackPath
            ));
        }
    }

    private Version GetVersionFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return Version.TryParse(fileName, out var version) ? version : null;
    }

    private string ComputeFileHash(string filePath)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
    }

    private bool ColumnDefinitionsEqual(JObject expected, JToken actual)
    {
        return
            expected["type"].ToString().Equals(actual["type"].ToString(), StringComparison.OrdinalIgnoreCase) &&
            expected["nullable"].ToString() == actual["nullable"].ToString() &&
            expected["default"].ToString() == actual["default"].ToString();
    }

    private string GenerateColumnDefinition(JObject column)
    {
        var sb = new StringBuilder();
        sb.Append(column["name"]);
        sb.Append(" ").Append(column["type"]);

        if (column["nullable"].ToString() == "NO")
            sb.Append(" NOT NULL");

        if (!string.IsNullOrEmpty(column["default"].ToString()))
            sb.Append(" DEFAULT ").Append(FormatDefaultValue(column["default"].ToString()));

        return sb.ToString();
    }

    private string GenerateCreateIndexCommand(string tableName, JObject index)
    {
        var sb = new StringBuilder();

        if (index["unique"].ToString() == "True")
            sb.Append("CREATE UNIQUE INDEX ");
        else
            sb.Append("CREATE INDEX ");

        sb.Append(index["name"]);
        sb.Append($" ON {tableName} ({string.Join(",", index["columns"])})");

        return sb.ToString() + ";";
    }

    private string FormatDefaultValue(string defaultValue)
    {
        if (defaultValue.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return "NULL";

        if (int.TryParse(defaultValue, out _) || decimal.TryParse(defaultValue, out _))
            return defaultValue;

        return $"'{defaultValue.Replace("'", "''")}'";
    }
    #endregion

    #region Database Operations
    private void EnsureVersionHistoryExists(MySqlConnection connection)
    {
        var createTableCommand = @"
            CREATE TABLE IF NOT EXISTS VersionHistory (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                VersionNumber VARCHAR(20) NOT NULL,
                DateApplied DATETIME NOT NULL,
                ActionType ENUM('UP', 'DOWN') NOT NULL,
                MigrationHash VARCHAR(64)
            ) ENGINE=InnoDB;";

        ExecuteScript(connection, createTableCommand);
    }

    private Version GetCurrentVersion(MySqlConnection connection)
    {
        var command = @"
            SELECT VersionNumber 
            FROM VersionHistory 
            WHERE ActionType = 'UP'
            AND VersionNumber NOT IN (
                SELECT VersionNumber 
                FROM VersionHistory 
                WHERE ActionType = 'DOWN'
            )
            ORDER BY DateApplied DESC 
            LIMIT 1";

        using (var cmd = new MySqlCommand(command, connection))
        {
            var result = cmd.ExecuteScalar();
            return result != null ? new Version(result.ToString()) : new Version(0, 0, 0);
        }
    }

    private void LogMigration(MySqlConnection connection, MySqlTransaction transaction, Version version, string actionType)
    {
        var hash = actionType == "UP"
            ? ComputeFileHash(_migrations[version].UpScriptPath)
            : null;

        var command = @"
            INSERT INTO VersionHistory (VersionNumber, DateApplied, ActionType, MigrationHash)
            VALUES (@version, @date, @actionType, @hash)";

        using (var cmd = new MySqlCommand(command, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@version", version.ToString());
            cmd.Parameters.AddWithValue("@date", DateTime.Now);
            cmd.Parameters.AddWithValue("@actionType", actionType);
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.ExecuteNonQuery();
        }
    }

    private void ExecuteScript(MySqlConnection connection, string script, MySqlTransaction transaction = null)
    {
        var commands = script.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var commandText in commands)
        {
            if (string.IsNullOrWhiteSpace(commandText)) continue;

            using (var cmd = new MySqlCommand(commandText, connection, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
    #endregion

    #region Logging
    private void Log(string message, bool isError = false)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(isError ? "ERROR" : "INFO")} - {message}";

        lock (_logFilePath)
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }

        if (isError)
        {
            Console.Error.WriteLine(logEntry);
        }
        else
        {
            Console.WriteLine(logEntry);
        }
    }
    #endregion
}
