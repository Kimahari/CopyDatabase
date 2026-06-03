namespace CopyDatabase.Core.Requests;

public enum SchemaObjectType
{
    Table,
    View
}

public sealed record GetSchemaObjectList : IRequest<string[]>
{
    public IDatabaseServerCredentials? ServerCredentials { get; set; }
    public string DatabaseName { get; set; } = "";
    public SchemaObjectType ObjectType { get; set; }
}

public sealed class GetSchemaObjectListCommandHandler : IRequestHandler<GetSchemaObjectList, string[]>
{
    private readonly IDbConnectionFactory connectionFactory;
    private readonly IConnectionStringBuilderFactory connectionStringBuilderFactory;

    public GetSchemaObjectListCommandHandler(IDbConnectionFactory connectionFactory, IConnectionStringBuilderFactory connectionStringBuilderFactory)
    {
        this.connectionFactory = connectionFactory;
        this.connectionStringBuilderFactory = connectionStringBuilderFactory;
    }

    public async Task<string[]> Handle(GetSchemaObjectList request, CancellationToken cancellationToken)
    {
        if (request.ServerCredentials is null || string.IsNullOrWhiteSpace(request.DatabaseName)) return Array.Empty<string>();

        var connectionString = connectionStringBuilderFactory
            .GetConnectionStringBuilder(DatabaseProvider.MsSQLServer)
            .BuildConnection(request.ServerCredentials, request.DatabaseName);

        using var connection = connectionFactory.GetDbConnection(connectionString, DatabaseProvider.MsSQLServer);
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = request.ObjectType switch
        {
            SchemaObjectType.Table => """
                SELECT TABLE_SCHEMA + '.' + TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """,
            SchemaObjectType.View => """
                SELECT TABLE_SCHEMA + '.' + TABLE_NAME
                FROM INFORMATION_SCHEMA.VIEWS
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """,
            _ => throw new NotImplementedException()
        };

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();

        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));

        return result.ToArray();
    }
}
