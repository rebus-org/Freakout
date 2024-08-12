using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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
    public static void AddOutboxCommand(this DbTransaction transaction, ICommandSerializer serializer, string schemaName, string tableName, object command, Dictionary<string, string> headers = null)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializedCommand = serializer.Serialize(command);

        var payload = serializedCommand.Payload;
        var headersToUse = serializedCommand.Headers;

        headers?.InsertInto(headersToUse);

        Insert(schemaName, tableName, transaction, HeaderSerializer.SerializeToString(headersToUse), payload);
    }

    /// <summary>
    /// Adds the given <paramref name="command"/> to the store as part of the SQL transaction. The command will be added to the store
    /// when the transaction is committed.
    /// </summary>
    public static async Task AddOutboxCommandAsync(this DbTransaction transaction, ICommandSerializer serializer, string schemaName, string tableName, object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializedCommand = serializer.Serialize(command);

        var payload = serializedCommand.Payload;
        var headersToUse = serializedCommand.Headers;

        headers?.InsertInto(headersToUse);

        await InsertAsync(schemaName, tableName, transaction, HeaderSerializer.SerializeToString(headersToUse), payload, cancellationToken);
    }

    static void Insert(string schemaName, string tableName, DbTransaction transaction, string headers, byte[] bytes)
    {
        var connection = transaction.Connection ?? throw new ArgumentException($"The {transaction} did not have a DbConnection on it!");

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;

        SetQueryAndParameters(schemaName, tableName, headers, bytes, cmd);

        cmd.ExecuteNonQuery();
    }

    static async Task InsertAsync(string schemaName, string tableName, DbTransaction transaction, string headers, byte[] bytes, CancellationToken cancellationToken)
    {
        var connection = transaction.Connection ?? throw new ArgumentException($"The {transaction} did not have a DbConnection on it!");

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;

        SetQueryAndParameters(schemaName, tableName, headers, bytes, cmd);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    static void SetQueryAndParameters(string schemaName, string tableName, string headers, byte[] bytes, DbCommand cmd)
    {
        cmd.CommandText = $"INSERT INTO [{schemaName}].[{tableName}] ([Id], [CreatedAt], [Headers], [Payload]) VALUES (@id, SYSDATETIMEOFFSET(), @headers, @payload)";
        cmd.Parameters.Add(new SqlParameter("id", SequentialGuidGenerator.Instance.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("headers", headers));
        cmd.Parameters.Add(new SqlParameter("payload", bytes));
    }
}