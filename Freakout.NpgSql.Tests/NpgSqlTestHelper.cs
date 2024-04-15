using Nito.AsyncEx.Synchronous;
using Nito.Disposables;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Freakout.NpgSql.Tests;

[SetUpFixture]
class NpgSqlTestHelper
{
    static readonly CollectionDisposable Disposables = new();

    static readonly Lazy<string> LazyConnectionString = new(() =>
    {
        var connectionString = Environment.GetEnvironmentVariable("NPGSQL_TEST_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        var builder = new PostgreSqlBuilder();
        var container = builder.Build();

        container.StartAsync().WaitAndUnwrapException();

        Disposables.Add(new Disposable(() => container.DisposeAsync()));

        return container.GetConnectionString();
    });

    public static string ConnectionString => LazyConnectionString.Value;

    [OneTimeTearDown]
    public void CleanUp() => Disposables.Dispose();

    public static void DropTable(string tableName) => DropTable("dbo", tableName);

    public static void DropTable(string schemaName, string tableName)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"DROP TABLE IF EXISTS ""{schemaName}"".""{tableName}"";";
        command.ExecuteNonQuery();
    }

}