
namespace CopyDatabase.Core.Requests;

public sealed record GetDatabaseList : IRequest<string[]> {
    public IDatabaseServerCredentials? ServerCredentials { get; set; }
}

public sealed class GetDatabaseListCommandHandler : IRequestHandler<GetDatabaseList, string[]> {
    private readonly IDbConnectionFactory connectionFactory;
    private readonly IConnectionStringBuilderFactory connectionStringBuilderFactory;

    public GetDatabaseListCommandHandler(IDbConnectionFactory connectionFactory, IConnectionStringBuilderFactory connectionStringBuilderFactory) {
        this.connectionFactory = connectionFactory;
        this.connectionStringBuilderFactory = connectionStringBuilderFactory;
    }

    public async Task<string[]> Handle(GetDatabaseList request, CancellationToken cancellationToken) {
        if (request.ServerCredentials is null) return Array.Empty<string>();

        var query = "SELECT name FROM sys.databases";
        
        var connectionString = connectionStringBuilderFactory.GetConnectionStringBuilder(DatabaseProvider.MsSQLServer).BuildConnection(request.ServerCredentials);
        using var connection = connectionFactory.GetDbConnection(connectionString, DatabaseProvider.MsSQLServer);

        try {
            await connection.OpenAsync(cancellationToken);
            using var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var result = new List<string>();

            while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));

            return result.ToArray();
        } catch (Exception) {
            throw;
        }
    }
}


