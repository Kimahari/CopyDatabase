namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed record SqlServerSchemaObject(string Schema, string Name, string Script)
{
    public string QualifiedName => $"{SqlServerSql.QuoteName(Schema)}.{SqlServerSql.QuoteName(Name)}";
    public string DisplayName => $"{Schema}.{Name}";
}

