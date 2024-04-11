using System;
using Microsoft.Data.SqlClient;
using Nito.AsyncEx.Synchronous;
using Nito.Disposables;
using NUnit.Framework;
using Testcontainers.MsSql;
using Testy;

namespace Freakout.Tests;

[SetUpFixture]
public abstract class MsSqlFixtureBase : FixtureBase
{
    static readonly CollectionDisposable Disposables = new();

    static readonly Lazy<string> LazyConnectionString = new(() =>
    {
        var connectionString = Environment.GetEnvironmentVariable("MSSQL_TEST_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        var builder = new MsSqlBuilder();
        var container = builder.Build();

        container.StartAsync().WaitAndUnwrapException();

        Disposables.Add(new Disposable(() => container.DisposeAsync()));

        return container.GetConnectionString();
    });

    protected string ConnectionString => LazyConnectionString.Value;

    [OneTimeTearDown]
    public void CleanUp() => Disposables.Dispose();

    protected void DropTable(string tableName) => DropTable("dbo", tableName);

    protected void DropTable(string schemaName, string tableName)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE [{schemaName}].[{tableName}]";
        command.ExecuteNonQuery();
    }
}