using CopyDatabase.Core.Factories;
using CopyDatabase.Core.Validation;

namespace CopyDatabase.Core.Requests;

public record TestDBConnection : IRequest<bool> {
    public IDatabaseServerCredentials? Credentials { get; set; }
}

public class TestDBConnectionHandler : IRequestHandler<TestDBConnection, bool> {

    public async Task<bool> Handle(TestDBConnection request, CancellationToken cancellationToken) {
        if (request.Credentials is null) return false;

        var x = new DatabaseServerCredentialValidator();
        var result = await x.ValidateAsync(request.Credentials);

        if (!result.IsValid) {
            throw new Exception($"{result.Errors.First().ErrorMessage}");
        }

        var builder = ConnectionStringBuilderFactory.GetConnectionStringBuilder();
        var connectionString = builder.BuildConnection(request.Credentials);
        using var connection = ConnectionFactory.GetDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return true;
    }
}