using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Voltyks.Application.Services.Background.Backup
{
    public class BackupResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TablesExported { get; set; }
        public long TotalRowsExported { get; set; }
        public Dictionary<string, long> TableRowCounts { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    public class BackupExecutor
    {
        private readonly string _connectionString;
        private readonly DatabaseBackupOptions _options;
        private readonly ILogger _logger;

        public BackupExecutor(string connectionString, DatabaseBackupOptions options, ILogger logger)
        {
            _connectionString = connectionString;
            _options = options;
            _logger = logger;
        }

        public async Task<BackupResult> ExecuteBackupAsync(CancellationToken ct)
        {
            var result = new BackupResult();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var backupDir = Path.IsPathRooted(_options.BackupPath)
                ? _options.BackupPath
                : Path.Combine(AppContext.BaseDirectory, _options.BackupPath);

            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var zipFileName = $"backup_{timestamp}.zip";
            var zipFilePath = Path.Combine(backupDir, zipFileName);

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                var tables = await GetAllTableNamesAsync(conn, ct);
                _logger.LogInformation("Found {Count} tables to backup", tables.Count);

                using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    // Export schema (both JSON and SQL)
                    await ExportSchemaJsonToZipAsync(conn, zip, ct);
                    await ExportSchemaSqlToZipAsync(conn, zip, ct);

                    // Export each table
                    foreach (var table in tables)
                    {
                        try
                        {
                            await ExportTableToZipAsync(conn, zip, table, result, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to export table {Table}", table);
                            result.Errors.Add($"Table {table}: {ex.Message}");
                        }
                    }

                    // Write manifest
                    var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
                    using var manifestStream = manifestEntry.Open();
                    await JsonSerializer.SerializeAsync(manifestStream, new
                    {
                        backupTimestamp = DateTime.UtcNow.ToString("O"),
                        databaseName = "VoltyksDB",
                        tablesExported = result.TablesExported,
                        totalRows = result.TotalRowsExported,
                        tableRowCounts = result.TableRowCounts,
                        errors = result.Errors
                    }, new JsonSerializerOptions { WriteIndented = true }, ct);
                }

                // Verify
                var verified = await VerifyBackupAsync(zipFilePath, tables, result.TableRowCounts, ct);
                result.Success = verified && result.Errors.Count == 0;
                result.FilePath = zipFilePath;
                result.FileSizeBytes = new FileInfo(zipFilePath).Length;

                // Enforce retention
                EnforceRetention(backupDir);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Fatal: {ex.Message}");
                _logger.LogError(ex, "Fatal error during backup");

                if (File.Exists(zipFilePath))
                {
                    try { File.Delete(zipFilePath); } catch { }
                }
            }

            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        private async Task<List<string>> GetAllTableNamesAsync(SqlConnection conn, CancellationToken ct)
        {
            var tables = new List<string>();
            using var cmd = new SqlCommand(
                "SELECT '[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']' FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME",
                conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add(reader.GetString(0));

            return tables;
        }

        private async Task ExportTableToZipAsync(
            SqlConnection conn, ZipArchive zip, string fullTableName,
            BackupResult result, CancellationToken ct)
        {
            // Get row count
            using var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {fullTableName}", conn);
            countCmd.CommandTimeout = _options.CommandTimeoutSeconds;
            var rowCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync(ct));

            var safeName = fullTableName.Replace("[", "").Replace("]", "");
            var entry = zip.CreateEntry($"data/{safeName}.json", CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            using var writer = new Utf8JsonWriter(entryStream, new JsonWriterOptions { Indented = false });

            writer.WriteStartArray();

            using var cmd = new SqlCommand($"SELECT * FROM {fullTableName}", conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(i => new { Name = reader.GetName(i), Type = reader.GetFieldType(i) })
                .ToList();

            long exportedRows = 0;
            while (await reader.ReadAsync(ct))
            {
                writer.WriteStartObject();
                for (int i = 0; i < columns.Count; i++)
                {
                    writer.WritePropertyName(columns[i].Name);
                    if (await reader.IsDBNullAsync(i, ct))
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        WriteValue(writer, reader, i, columns[i].Type);
                    }
                }
                writer.WriteEndObject();
                exportedRows++;

                if (exportedRows % _options.BatchSize == 0)
                    await writer.FlushAsync(ct);
            }

            writer.WriteEndArray();
            await writer.FlushAsync(ct);

            result.TableRowCounts[fullTableName] = exportedRows;
            result.TotalRowsExported += exportedRows;
            result.TablesExported++;

            _logger.LogDebug("Exported {Table}: {Rows} rows", fullTableName, exportedRows);
        }

        private static void WriteValue(Utf8JsonWriter writer, SqlDataReader reader, int index, Type type)
        {
            if (type == typeof(byte[]))
            {
                var bytes = (byte[])reader.GetValue(index);
                writer.WriteStringValue(Convert.ToBase64String(bytes));
            }
            else if (type == typeof(DateTime))
            {
                writer.WriteStringValue(reader.GetDateTime(index).ToString("O"));
            }
            else if (type == typeof(DateTimeOffset))
            {
                writer.WriteStringValue(reader.GetDateTimeOffset(index).ToString("O"));
            }
            else if (type == typeof(bool))
            {
                writer.WriteBooleanValue(reader.GetBoolean(index));
            }
            else if (type == typeof(int))
            {
                writer.WriteNumberValue(reader.GetInt32(index));
            }
            else if (type == typeof(long))
            {
                writer.WriteNumberValue(reader.GetInt64(index));
            }
            else if (type == typeof(short))
            {
                writer.WriteNumberValue(reader.GetInt16(index));
            }
            else if (type == typeof(byte))
            {
                writer.WriteNumberValue(reader.GetByte(index));
            }
            else if (type == typeof(decimal))
            {
                writer.WriteNumberValue(reader.GetDecimal(index));
            }
            else if (type == typeof(double))
            {
                writer.WriteNumberValue(reader.GetDouble(index));
            }
            else if (type == typeof(float))
            {
                writer.WriteNumberValue(reader.GetFloat(index));
            }
            else if (type == typeof(Guid))
            {
                writer.WriteStringValue(reader.GetGuid(index).ToString());
            }
            else if (type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)reader.GetValue(index);
                writer.WriteStringValue(ts.ToString("c"));
            }
            else
            {
                writer.WriteStringValue(reader.GetValue(index)?.ToString() ?? string.Empty);
            }
        }

        #region Schema Export - JSON

        private async Task ExportSchemaJsonToZipAsync(SqlConnection conn, ZipArchive zip, CancellationToken ct)
        {
            var entry = zip.CreateEntry("schema/database_schema.json", CompressionLevel.Optimal);
            using var entryStream = entry.Open();

            var schema = new
            {
                exportedAt = DateTime.UtcNow.ToString("O"),
                databaseName = "VoltyksDB",
                columns = await GetColumnsAsync(conn, ct),
                indexes = await GetIndexesAsync(conn, ct),
                foreignKeys = await GetForeignKeysAsync(conn, ct)
            };

            await JsonSerializer.SerializeAsync(entryStream, schema,
                new JsonSerializerOptions { WriteIndented = true }, ct);
        }

        private async Task<List<Dictionary<string, object?>>> GetColumnsAsync(SqlConnection conn, CancellationToken ct)
        {
            const string sql = @"
                SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION,
                       DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION,
                       NUMERIC_SCALE, IS_NULLABLE, COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION";

            return await QueryToDictionaryListAsync(conn, sql, ct);
        }

        private async Task<List<Dictionary<string, object?>>> GetIndexesAsync(SqlConnection conn, CancellationToken ct)
        {
            const string sql = @"
                SELECT i.name AS IndexName, t.name AS TableName, s.name AS SchemaName,
                       c.name AS ColumnName, i.is_primary_key AS IsPrimaryKey,
                       i.is_unique AS IsUnique, i.type_desc AS IndexType
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE i.name IS NOT NULL
                ORDER BY s.name, t.name, i.name, ic.key_ordinal";

            return await QueryToDictionaryListAsync(conn, sql, ct);
        }

        private async Task<List<Dictionary<string, object?>>> GetForeignKeysAsync(SqlConnection conn, CancellationToken ct)
        {
            const string sql = @"
                SELECT fk.name AS ForeignKeyName,
                       sp.name AS ParentSchema, tp.name AS ParentTable, cp.name AS ParentColumn,
                       sr.name AS ReferencedSchema, tr.name AS ReferencedTable, cr.name AS ReferencedColumn,
                       fk.delete_referential_action_desc AS DeleteAction,
                       fk.update_referential_action_desc AS UpdateAction
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
                INNER JOIN sys.schemas sp ON tp.schema_id = sp.schema_id
                INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
                INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
                INNER JOIN sys.schemas sr ON tr.schema_id = sr.schema_id
                INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
                ORDER BY fk.name, fkc.constraint_column_id";

            return await QueryToDictionaryListAsync(conn, sql, ct);
        }

        private async Task<List<Dictionary<string, object?>>> QueryToDictionaryListAsync(
            SqlConnection conn, string sql, CancellationToken ct)
        {
            var results = new List<Dictionary<string, object?>>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var columns = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();

            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in columns)
                {
                    var val = reader[col];
                    row[col] = val == DBNull.Value ? null : val;
                }
                results.Add(row);
            }
            return results;
        }

        #endregion

        #region Schema Export - SQL Script

        private async Task ExportSchemaSqlToZipAsync(SqlConnection conn, ZipArchive zip, CancellationToken ct)
        {
            var entry = zip.CreateEntry("schema/schema.sql", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);

            await writer.WriteLineAsync($"-- Database Schema Export: VoltyksDB");
            await writer.WriteLineAsync($"-- Generated: {DateTime.UtcNow:O}");
            await writer.WriteLineAsync();

            // Get all tables
            var tables = await GetTableSchemaInfoAsync(conn, ct);

            foreach (var table in tables)
            {
                await WriteCreateTableAsync(writer, conn, table.Schema, table.Name, ct);
            }

            // Foreign keys (separate pass so all tables exist first)
            await WriteForeignKeysAsync(writer, conn, ct);

            // Indexes (non-PK, non-unique-constraint)
            await WriteIndexesAsync(writer, conn, ct);

            await writer.FlushAsync(ct);
        }

        private async Task<List<(string Schema, string Name)>> GetTableSchemaInfoAsync(SqlConnection conn, CancellationToken ct)
        {
            var result = new List<(string, string)>();
            using var cmd = new SqlCommand(
                "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME",
                conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                result.Add((reader.GetString(0), reader.GetString(1)));

            return result;
        }

        private async Task WriteCreateTableAsync(StreamWriter writer, SqlConnection conn, string schema, string table, CancellationToken ct)
        {
            await writer.WriteLineAsync($"-- Table: [{schema}].[{table}]");
            await writer.WriteLineAsync($"CREATE TABLE [{schema}].[{table}] (");

            // Columns
            var columns = new List<string>();
            using (var cmd = new SqlCommand(@"
                SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                       c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.IS_NULLABLE, c.COLUMN_DEFAULT,
                       COLUMNPROPERTY(OBJECT_ID(@schema + '.' + @table), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION", conn))
            {
                cmd.CommandTimeout = _options.CommandTimeoutSeconds;
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);

                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var colName = reader.GetString(0);
                    var dataType = reader.GetString(1);
                    var maxLen = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    var numPrec = reader.IsDBNull(3) ? (byte?)null : reader.GetByte(3);
                    var numScale = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                    var isNullable = reader.GetString(5) == "YES";
                    var colDefault = reader.IsDBNull(6) ? null : reader.GetString(6);
                    var isIdentity = !reader.IsDBNull(7) && reader.GetInt32(7) == 1;

                    var typeDef = FormatColumnType(dataType, maxLen, numPrec, numScale);
                    var identityStr = isIdentity ? " IDENTITY(1,1)" : "";
                    var nullStr = isNullable ? " NULL" : " NOT NULL";
                    var defaultStr = colDefault != null ? $" DEFAULT {colDefault}" : "";

                    columns.Add($"    [{colName}] {typeDef}{identityStr}{nullStr}{defaultStr}");
                }
            }

            // Primary key
            string? pkConstraint = null;
            using (var cmd = new SqlCommand(@"
                SELECT i.name AS PkName, c.name AS ColName
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.is_primary_key = 1
                  AND i.object_id = OBJECT_ID(@fullName)
                ORDER BY ic.key_ordinal", conn))
            {
                cmd.CommandTimeout = _options.CommandTimeoutSeconds;
                cmd.Parameters.AddWithValue("@fullName", $"[{schema}].[{table}]");

                using var reader = await cmd.ExecuteReaderAsync(ct);
                string? pkName = null;
                var pkCols = new List<string>();
                while (await reader.ReadAsync(ct))
                {
                    pkName ??= reader.GetString(0);
                    pkCols.Add($"[{reader.GetString(1)}]");
                }

                if (pkName != null)
                    pkConstraint = $"    CONSTRAINT [{pkName}] PRIMARY KEY ({string.Join(", ", pkCols)})";
            }

            if (pkConstraint != null)
                columns.Add(pkConstraint);

            await writer.WriteLineAsync(string.Join(",\n", columns));
            await writer.WriteLineAsync(");");
            await writer.WriteLineAsync("GO");
            await writer.WriteLineAsync();
        }

        private static string FormatColumnType(string dataType, int? maxLen, byte? numPrec, int? numScale)
        {
            var upper = dataType.ToUpperInvariant();
            return upper switch
            {
                "NVARCHAR" or "NCHAR" => maxLen == -1 ? $"{upper}(MAX)" : $"{upper}({maxLen})",
                "VARCHAR" or "CHAR" => maxLen == -1 ? $"{upper}(MAX)" : $"{upper}({maxLen})",
                "VARBINARY" => maxLen == -1 ? "VARBINARY(MAX)" : $"VARBINARY({maxLen})",
                "DECIMAL" or "NUMERIC" => $"{upper}({numPrec},{numScale})",
                "FLOAT" => numPrec.HasValue && numPrec != 53 ? $"FLOAT({numPrec})" : "FLOAT",
                _ => upper
            };
        }

        private async Task WriteForeignKeysAsync(StreamWriter writer, SqlConnection conn, CancellationToken ct)
        {
            const string sql = @"
                SELECT fk.name AS FkName,
                       sp.name AS ParentSchema, tp.name AS ParentTable, cp.name AS ParentColumn,
                       sr.name AS RefSchema, tr.name AS RefTable, cr.name AS RefColumn,
                       fk.delete_referential_action_desc AS DeleteAction,
                       fk.update_referential_action_desc AS UpdateAction
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
                INNER JOIN sys.schemas sp ON tp.schema_id = sp.schema_id
                INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
                INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
                INNER JOIN sys.schemas sr ON tr.schema_id = sr.schema_id
                INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
                ORDER BY fk.name, fkc.constraint_column_id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            // Group FK columns by FK name
            var fks = new Dictionary<string, (string ParentSchema, string ParentTable, List<string> ParentCols,
                string RefSchema, string RefTable, List<string> RefCols, string DeleteAction, string UpdateAction)>();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var fkName = reader.GetString(0);
                if (!fks.ContainsKey(fkName))
                {
                    fks[fkName] = (reader.GetString(1), reader.GetString(2), new List<string>(),
                        reader.GetString(4), reader.GetString(5), new List<string>(),
                        reader.GetString(7), reader.GetString(8));
                }
                fks[fkName].ParentCols.Add($"[{reader.GetString(3)}]");
                fks[fkName].RefCols.Add($"[{reader.GetString(6)}]");
            }

            if (fks.Count > 0)
            {
                await writer.WriteLineAsync("-- Foreign Keys");
                foreach (var (fkName, fk) in fks)
                {
                    var deleteAction = fk.DeleteAction == "NO_ACTION" ? "" : $" ON DELETE {fk.DeleteAction.Replace("_", " ")}";
                    var updateAction = fk.UpdateAction == "NO_ACTION" ? "" : $" ON UPDATE {fk.UpdateAction.Replace("_", " ")}";

                    await writer.WriteLineAsync(
                        $"ALTER TABLE [{fk.ParentSchema}].[{fk.ParentTable}] ADD CONSTRAINT [{fkName}] " +
                        $"FOREIGN KEY ({string.Join(", ", fk.ParentCols)}) " +
                        $"REFERENCES [{fk.RefSchema}].[{fk.RefTable}] ({string.Join(", ", fk.RefCols)}){deleteAction}{updateAction};");
                }
                await writer.WriteLineAsync("GO");
                await writer.WriteLineAsync();
            }
        }

        private async Task WriteIndexesAsync(StreamWriter writer, SqlConnection conn, CancellationToken ct)
        {
            const string sql = @"
                SELECT i.name AS IndexName, s.name AS SchemaName, t.name AS TableName,
                       c.name AS ColumnName, i.is_unique AS IsUnique, i.type_desc AS IndexType
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE i.is_primary_key = 0 AND i.type > 0 AND i.name IS NOT NULL
                ORDER BY s.name, t.name, i.name, ic.key_ordinal";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = _options.CommandTimeoutSeconds;

            var indexes = new Dictionary<string, (string Schema, string Table, List<string> Cols, bool IsUnique, string Type)>();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var idxName = reader.GetString(0);
                if (!indexes.ContainsKey(idxName))
                {
                    indexes[idxName] = (reader.GetString(1), reader.GetString(2), new List<string>(),
                        reader.GetBoolean(4), reader.GetString(5));
                }
                indexes[idxName].Cols.Add($"[{reader.GetString(3)}]");
            }

            if (indexes.Count > 0)
            {
                await writer.WriteLineAsync("-- Indexes");
                foreach (var (idxName, idx) in indexes)
                {
                    var unique = idx.IsUnique ? "UNIQUE " : "";
                    var type = idx.Type == "NONCLUSTERED" ? "NONCLUSTERED " : "";
                    await writer.WriteLineAsync(
                        $"CREATE {unique}{type}INDEX [{idxName}] ON [{idx.Schema}].[{idx.Table}] ({string.Join(", ", idx.Cols)});");
                }
                await writer.WriteLineAsync("GO");
                await writer.WriteLineAsync();
            }
        }

        #endregion

        #region Verification

        private async Task<bool> VerifyBackupAsync(
            string zipFilePath, List<string> expectedTables,
            Dictionary<string, long> expectedRowCounts, CancellationToken ct)
        {
            _logger.LogInformation("Verifying backup...");

            var fileInfo = new FileInfo(zipFilePath);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                _logger.LogError("Backup file missing or empty: {Path}", zipFilePath);
                return false;
            }

            try
            {
                using var zip = ZipFile.OpenRead(zipFilePath);

                // Check schema entries
                if (zip.GetEntry("schema/database_schema.json") == null)
                {
                    _logger.LogError("Backup missing JSON schema entry");
                    return false;
                }
                if (zip.GetEntry("schema/schema.sql") == null)
                {
                    _logger.LogError("Backup missing SQL schema entry");
                    return false;
                }

                // Check manifest
                if (zip.GetEntry("manifest.json") == null)
                {
                    _logger.LogError("Backup missing manifest");
                    return false;
                }

                // Verify each table entry and row counts
                int verifiedTables = 0;
                foreach (var tableName in expectedTables)
                {
                    var safeName = tableName.Replace("[", "").Replace("]", "");
                    var entry = zip.GetEntry($"data/{safeName}.json");
                    if (entry == null)
                    {
                        _logger.LogWarning("Missing backup entry for table {Table}", tableName);
                        continue;
                    }

                    using var stream = entry.Open();
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                    var actualCount = doc.RootElement.GetArrayLength();

                    if (expectedRowCounts.TryGetValue(tableName, out var expected) && actualCount != expected)
                    {
                        _logger.LogWarning("Row count mismatch for {Table}: expected {Expected}, got {Actual}",
                            tableName, expected, actualCount);
                    }

                    verifiedTables++;
                }

                _logger.LogInformation("Verification complete: {Verified}/{Total} tables verified",
                    verifiedTables, expectedTables.Count);

                return verifiedTables == expectedTables.Count;
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Backup ZIP file is corrupt");
                return false;
            }
        }

        #endregion

        #region Retention

        public void EnforceRetention(string backupDirectory)
        {
            if (!Directory.Exists(backupDirectory))
                return;

            var backupFiles = Directory.GetFiles(backupDirectory, "backup_*.zip")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (backupFiles.Count <= _options.RetentionCount)
                return;

            var toDelete = backupFiles.Skip(_options.RetentionCount).ToList();
            foreach (var file in toDelete)
            {
                try
                {
                    File.Delete(file.FullName);
                    _logger.LogInformation("Deleted old backup: {File}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup: {File}", file.Name);
                }
            }
        }

        #endregion
    }
}
