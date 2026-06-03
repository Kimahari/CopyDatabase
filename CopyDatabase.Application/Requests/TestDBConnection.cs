using System.Data;

using FluentValidation;

namespace CopyDatabase.Core.Requests;

public sealed record TestDBConnection : IRequest<bool> {
    public IDatabaseServerCredentials? Credentials { get; set; }
}

public sealed class TestDBConnectionHandler : IRequestHandler<TestDBConnection, bool> {
    private readonly AbstractValidator<IDatabaseServerCredentials> credentialsValidator;
    private readonly IDbConnectionFactory connectionFactory;
    private readonly IConnectionStringBuilderFactory connectionStringBuilderFactory;

    public TestDBConnectionHandler(AbstractValidator<IDatabaseServerCredentials> credentialsValidator, IDbConnectionFactory connectionFactory, IConnectionStringBuilderFactory connectionStringBuilderFactory) {
        this.credentialsValidator = credentialsValidator;
        this.connectionFactory = connectionFactory;
        this.connectionStringBuilderFactory = connectionStringBuilderFactory;
    }

    public async Task<bool> Handle(TestDBConnection request, CancellationToken cancellationToken) {
        if (request.Credentials is null) return false;

        var provider = DatabaseProvider.MsSQLServer;

        var result = await credentialsValidator.ValidateAsync(request.Credentials, cancellationToken);

        if (!result.IsValid) {
            throw new Exception($"{result.Errors.First().ErrorMessage}");
        }

        var builder = connectionStringBuilderFactory.GetConnectionStringBuilder(provider);
        var connectionString = builder.BuildConnection(request.Credentials);
        using var connection = connectionFactory.GetDbConnection(connectionString, provider);
        await connection.OpenAsync(cancellationToken);
        return connection.State == ConnectionState.Open;
    }
}