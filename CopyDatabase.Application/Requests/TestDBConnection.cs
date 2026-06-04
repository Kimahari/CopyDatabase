using System.Data;

using FluentValidation;

namespace CopyDatabase.Core.Requests;

public sealed record TestDBConnection : IRequest<bool>
{
    public IDatabaseServerCredentials? Credentials { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MsSQLServer;
}

public interface IDatabaseConnectionTester
{
    Task<bool> TestConnectionAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken);
}

public interface IDatabaseConnectionTesterFactory
{
    IDatabaseConnectionTester GetDatabaseConnectionTester(DatabaseProvider provider);
}

public sealed class TestDBConnectionHandler : IRequestHandler<TestDBConnection, bool>
{
    private readonly AbstractValidator<IDatabaseServerCredentials> credentialsValidator;
    private readonly IDatabaseConnectionTesterFactory connectionTesterFactory;

    public TestDBConnectionHandler(
        AbstractValidator<IDatabaseServerCredentials> credentialsValidator,
        IDatabaseConnectionTesterFactory connectionTesterFactory)
    {
        this.credentialsValidator = credentialsValidator;
        this.connectionTesterFactory = connectionTesterFactory;
    }

    public async Task<bool> Handle(TestDBConnection request, CancellationToken cancellationToken)
    {
        if (request.Credentials is null) return false;

        var result = await credentialsValidator.ValidateAsync(request.Credentials, cancellationToken);

        if (!result.IsValid)
        {
            throw new Exception($"{result.Errors.First().ErrorMessage}");
        }

        var connectionTester = connectionTesterFactory.GetDatabaseConnectionTester(request.Provider);
        return await connectionTester.TestConnectionAsync(request.Credentials, cancellationToken);
    }
}
