using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;
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

    static void SetQueryAndParameters(string headers, byte[] bytes, DbCommand cmd, NpgsqlFreakoutConfiguration configuration)
    {
        cmd.CommandText = $@"INSERT INTO ""{configuration.SchemaName}"".""{configuration.TableName}"" (""id"", ""created_at"", ""headers"", ""payload"") VALUES (@id, CURRENT_TIMESTAMP, @headers, @payload);";

        cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = SequentialGuidGenerator.Instance.NewGuid() });
        cmd.Parameters.Add(new NpgsqlParameter("headers", NpgsqlDbType.Jsonb) { Value = headers });
        cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Bytea) { Value = bytes });
    }

    static NpgsqlFreakoutConfiguration GetConfiguration() => Globals.Get<FreakoutConfiguration>() as NpgsqlFreakoutConfiguration
                                                             ?? throw new InvalidOperationException("Could not retrieve global, PostgreSQL-specific Freakout configuration");
}