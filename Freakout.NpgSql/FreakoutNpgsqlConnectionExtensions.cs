using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Freakout.NpgSql.Internals;
using Freakout.Serialization;
using Npgsql;
using NpgsqlTypes;
using SequentialGuid;
// ReSharper disable UseAwaitUsing

namespace Freakout.NpgSql;

/// <summary>
/// Relevant extension methods for working with store commands in Microsoft SQL Server
/// </summary>
public static class FreakoutNpgsqlConnectionExtensions
{
    /// <summary>
    /// Adds the given <paramref name="command"/> to the store as part of the SQL transaction. The command will be added to the store
    /// when the transaction is committed.
    /// </summary>
    public static void AddOutboxCommand(this DbTransaction transaction, string schemaName, string tableName, ICommandSerializer serializer, object command, Dictionary<string, string> headers = null)
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
    public static async Task AddOutboxCommandAsync(this DbTransaction transaction, string schemaName, string tableName, ICommandSerializer serializer, object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
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
        cmd.CommandText = $@"INSERT INTO ""{schemaName}"".""{tableName}"" (""id"", ""created_at"", ""headers"", ""payload"") VALUES (@id, CURRENT_TIMESTAMP, @headers, @payload);";

        cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = SequentialGuidGenerator.Instance.NewGuid() });
        cmd.Parameters.Add(new NpgsqlParameter("headers", NpgsqlDbType.Jsonb) { Value = headers });
        cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Bytea) { Value = bytes });
    }
}