
namespace CopyDatabase.Core.Requests;

public record GetDatabaseList : IRequest<string[]> {
    public IDatabaseServerCredentials? ServerCredentials { get; set; }
}

public class GetDatabaseListCommandHandler : IRequestHandler<GetDatabaseList, string[]> {
    public GetDatabaseListCommandHandler() { }

    public async Task<string[]> Handle(GetDatabaseList request, CancellationToken cancellationToken) {
        if (request.ServerCredentials is null) return Array.Empty<string>();

        var query = "SELECT name FROM sys.databases";
        var f = Factories.ConnectionStringBuilderFactory.GetConnectionStringBuilder().BuildConnection(request.ServerCredentials);
        using var connection = Factories.ConnectionFactory.GetDbConnection(f);

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


