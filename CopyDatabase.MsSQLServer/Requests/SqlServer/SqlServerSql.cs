namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal static class SqlServerSql
{
    public static string QuoteName(string value) => $"[{value.Replace("]", "]]")}]";

    public static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    public const string Tables = @"
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME NOT IN ('sysdiagrams')
  AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;";

    public const string Views = @"
SELECT t.TABLE_SCHEMA, t.TABLE_NAME, m.definition
FROM INFORMATION_SCHEMA.TABLES t
INNER JOIN sys.sql_modules m ON m.object_id = OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME)
WHERE t.TABLE_NAME NOT IN ('sysdiagrams')
  AND t.TABLE_TYPE = 'VIEW'
ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME;";

    public const string Routines = @"
SELECT r.SPECIFIC_SCHEMA, r.SPECIFIC_NAME, m.definition
FROM INFORMATION_SCHEMA.ROUTINES r
INNER JOIN sys.sql_modules m ON m.object_id = OBJECT_ID(r.SPECIFIC_SCHEMA + '.' + r.SPECIFIC_NAME)
ORDER BY r.SPECIFIC_SCHEMA, r.SPECIFIC_NAME;";

    public const string ForeignKeys = @"
SELECT
    SCHEMA_NAME(fk.schema_id),
    fk.name,
    'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) +
    ' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(fk.name) +
    ' FOREIGN KEY (' +
        STUFF((
            SELECT ', ' + QUOTENAME(parent_column.name)
            FROM sys.foreign_key_columns foreign_key_column
            INNER JOIN sys.columns parent_column
                ON parent_column.object_id = foreign_key_column.parent_object_id
               AND parent_column.column_id = foreign_key_column.parent_column_id
            WHERE foreign_key_column.constraint_object_id = fk.object_id
            ORDER BY foreign_key_column.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') +
    ') REFERENCES ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.referenced_object_id)) + '.' + QUOTENAME(OBJECT_NAME(fk.referenced_object_id)) +
    ' (' +
        STUFF((
            SELECT ', ' + QUOTENAME(referenced_column.name)
            FROM sys.foreign_key_columns foreign_key_column
            INNER JOIN sys.columns referenced_column
                ON referenced_column.object_id = foreign_key_column.referenced_object_id
               AND referenced_column.column_id = foreign_key_column.referenced_column_id
            WHERE foreign_key_column.constraint_object_id = fk.object_id
            ORDER BY foreign_key_column.constraint_column_id
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') +
    ')' +
    CASE fk.delete_referential_action
        WHEN 1 THEN ' ON DELETE CASCADE'
        WHEN 2 THEN ' ON DELETE SET NULL'
        WHEN 3 THEN ' ON DELETE SET DEFAULT'
        ELSE ''
    END +
    CASE fk.update_referential_action
        WHEN 1 THEN ' ON UPDATE CASCADE'
        WHEN 2 THEN ' ON UPDATE SET NULL'
        WHEN 3 THEN ' ON UPDATE SET DEFAULT'
        ELSE ''
    END +
    CASE WHEN fk.is_not_for_replication = 1 THEN ' NOT FOR REPLICATION' ELSE '' END +
    ';' + CHAR(13) +
    'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) +
    ' CHECK CONSTRAINT ' + QUOTENAME(fk.name) + ';'
FROM sys.foreign_keys fk
INNER JOIN sys.tables parent_table ON parent_table.object_id = fk.parent_object_id
INNER JOIN sys.tables referenced_table ON referenced_table.object_id = fk.referenced_object_id
WHERE parent_table.is_ms_shipped = 0
  AND referenced_table.is_ms_shipped = 0
  AND parent_table.name NOT IN ('sysdiagrams')
  AND referenced_table.name NOT IN ('sysdiagrams')
ORDER BY SCHEMA_NAME(fk.schema_id), fk.name;";

    public const string CreateTableScript = @"
DECLARE @table_name SYSNAME
SELECT @table_name = '{Schema}.{Name}'

DECLARE
      @object_name SYSNAME
    , @object_id INT

SELECT
      @object_name = '[' + s.name + '].[' + o.name + ']'
    , @object_id = o.[object_id]
FROM sys.objects o WITH (NOWAIT)
JOIN sys.schemas s WITH (NOWAIT) ON o.[schema_id] = s.[schema_id]
WHERE s.name + '.' + o.name = @table_name
    AND o.[type] = 'U'
    AND o.is_ms_shipped = 0

DECLARE @SQL NVARCHAR(MAX) = ''

SELECT @SQL = 'CREATE TABLE ' + @object_name + CHAR(13) + '(' + CHAR(13) + STUFF((
    SELECT CHAR(9) + ', [' + c.name + '] ' +
        CASE WHEN c.is_computed = 1
            THEN 'AS ' + cc.[definition]
            ELSE UPPER(tp.name) +
                CASE WHEN tp.name IN ('varchar', 'char', 'varbinary', 'binary')
                       THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR(5)) END + ')'
                     WHEN tp.name IN ('nvarchar', 'nchar', 'ntext')
                       THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length / 2 AS VARCHAR(5)) END + ')'
                     WHEN tp.name IN ('datetime2', 'time2', 'datetimeoffset')
                       THEN '(' + CAST(c.scale AS VARCHAR(5)) + ')'
                     WHEN tp.name = 'decimal'
                       THEN '(' + CAST(c.[precision] AS VARCHAR(5)) + ',' + CAST(c.scale AS VARCHAR(5)) + ')'
                    ELSE ''
                END +
                CASE WHEN c.is_nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END +
                CASE WHEN dc.[definition] IS NOT NULL THEN ' DEFAULT' + dc.[definition] ELSE '' END +
                CASE WHEN ic.is_identity = 1 THEN ' IDENTITY(' + CAST(ISNULL(ic.seed_value, '0') AS VARCHAR(20)) + ',' + CAST(ISNULL(ic.increment_value, '1') AS VARCHAR(20)) + ')' ELSE '' END
        END + CHAR(13)
    FROM sys.columns c WITH (NOWAIT)
    JOIN sys.types tp WITH (NOWAIT) ON c.user_type_id = tp.user_type_id
    LEFT JOIN sys.computed_columns cc WITH (NOWAIT) ON c.[object_id] = cc.[object_id] AND c.column_id = cc.column_id
    LEFT JOIN sys.default_constraints dc WITH (NOWAIT) ON c.default_object_id != 0 AND c.[object_id] = dc.parent_object_id AND c.column_id = dc.parent_column_id
    LEFT JOIN sys.identity_columns ic WITH (NOWAIT) ON c.is_identity = 1 AND c.[object_id] = ic.[object_id] AND c.column_id = ic.column_id
    WHERE c.[object_id] = @object_id
    ORDER BY c.column_id
    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, CHAR(9) + ' ')
    + ISNULL((SELECT CHAR(9) + ', CONSTRAINT [' + k.name + '] PRIMARY KEY NONCLUSTERED (' +
                    (SELECT STUFF((
                         SELECT ', [' + c.name + '] ' + CASE WHEN ic.is_descending_key = 1 THEN 'DESC' ELSE 'ASC' END
                         FROM sys.index_columns ic WITH (NOWAIT)
                         JOIN sys.columns c WITH (NOWAIT) ON c.[object_id] = ic.[object_id] AND c.column_id = ic.column_id
                         WHERE ic.is_included_column = 0
                             AND ic.[object_id] = k.parent_object_id
                             AND ic.index_id = k.unique_index_id
                         FOR XML PATH(N''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''))
            + ')' + CHAR(13)
            FROM sys.key_constraints k WITH (NOWAIT)
            WHERE k.parent_object_id = @object_id
                AND k.[type] = 'PK'), '') + ')'  + CHAR(13)

SELECT @SQL;";
}

