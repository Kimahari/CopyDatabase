namespace CopyDatabase.Core;

/// <summary>
/// Contract to define a connection string builder factory.
/// </summary>
public interface IConnectionStringBuilderFactory {

    /// <summary>
    /// Gets a connection string builder for a given database provider.
    /// </summary>
    /// <param name="databaseProvider"></param>
    /// <returns></returns>
    IConnectionStringBuilder GetConnectionStringBuilder(DatabaseProvider databaseProvider);
}