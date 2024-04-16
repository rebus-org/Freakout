using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;
using Freakout.MsSql.Internals;
using Freakout.Serialization;
using Microsoft.Data.SqlClient;
using SequentialGuid;

// ReSharper disable UseAwaitUsing

namespace Freakout.MsSql;

/// <summary>
/// Relevant extension methods for working with store commands in Microsoft SQL Server
/// </summary>
public static class FreakoutSqlConnectionExtensions
{
    /// <summary>
    /// Adds the given <paramref name="command"/> to the store as part of the SQL transaction. The command will be added to the store
    /// when the transaction is committed.
    /// </summary>
    public static void AddOutboxCommand(this DbTransaction transaction, object command, Dictionary<string, string> headers = null)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializer = Globals.Get<FreakoutConfiguration>().CommandSerializer;
        var serializedCommand = serializer.Serialize(command);

        var payload = serializedCommand.Payload;
        var headersToUse = serializedCommand.Headers;

        headers?.InsertInto(headersToUse);

        Insert(transaction, HeaderSerializer.SerializeToString(headersToUse), payload);
    }

    /// <summary>
    /// Adds the given <paramref name="command"/> to the store as part of the SQL transaction. The command will be added to the store
    /// when the transaction is committed.
    /// </summary>
    public static async Task AddOutboxCommandAsync(this DbTransaction transaction, object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializer = Globals.Get<FreakoutConfiguration>().CommandSerializer;
        var serializedCommand = serializer.Serialize(command);

        var payload = serializedCommand.Payload;
        var headersToUse = serializedCommand.Headers;

        headers?.InsertInto(headersToUse);

        await InsertAsync(transaction, HeaderSerializer.SerializeToString(headersToUse), payload, cancellationToken);
    }

    static void Insert(DbTransaction transaction, string headers, byte[] bytes)
    {
        var configuration = GetConfiguration();

        var connection = transaction.Connection ?? throw new ArgumentException($"The {transaction} did not have a DbConnection on it!");

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;

        SetQueryAndParameters(headers, bytes, cmd, configuration);

        cmd.ExecuteNonQuery();
    }

    static async Task InsertAsync(DbTransaction transaction, string headers, byte[] bytes, CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();

        var connection = transaction.Connection ?? throw new ArgumentException($"The {transaction} did not have a DbConnection on it!");

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;

        SetQueryAndParameters(headers, bytes, cmd, configuration);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    static void SetQueryAndParameters(string headers, byte[] bytes, DbCommand cmd, MsSqlFreakoutConfiguration configuration)
    {
        cmd.CommandText = $"INSERT INTO [{configuration.SchemaName}].[{configuration.TableName}] ([Id], [CreatedAt], [Headers], [Payload]) VALUES (@id, SYSDATETIMEOFFSET(), @headers, @payload)";
        cmd.Parameters.Add(new SqlParameter("id", SequentialGuidGenerator.Instance.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("headers", headers));
        cmd.Parameters.Add(new SqlParameter("payload", bytes));
    }

    static MsSqlFreakoutConfiguration GetConfiguration() => Globals.Get<FreakoutConfiguration>() as MsSqlFreakoutConfiguration
                                                            ?? throw new InvalidOperationException("Could not retrieve global, MSSQL-specific Freakout configuration");
}