using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Freakout.Internals;
using Freakout.Serialization;
using Microsoft.Data.SqlClient;

namespace Freakout.MsSql;

public static class SqlConnectionExtensions
{
    public static async Task AddOutboxCommandAsync(this DbTransaction transaction, object command, CancellationToken cancellationToken = default)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (command == null) throw new ArgumentNullException(nameof(command));

        var serializer = Globals.Get<ICommandSerializer>();
        var serializedCommand = serializer.Serialize(command);

        var type = serializedCommand.TypeHeader;
        var payload = serializedCommand.Payload;

        var headers = new Dictionary<string, string> { [HeaderKeys.Type] = type };

        await Insert(transaction, HeaderSerializer.SerializeToString(headers), payload, cancellationToken);
    }

    static async Task Insert(DbTransaction transaction, string headers, byte[] bytes, CancellationToken cancellationToken)
    {
        var connection = transaction.Connection;

        using var cmd = connection.CreateCommand();

        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO [OutboxCommands] ([Id], [Time], [Headers], [Payload]) VALUES (@id, SYSDATETIMEOFFSET(), @headers, @payload)";
        cmd.Parameters.Add(new SqlParameter("id", Guid.NewGuid()));
        cmd.Parameters.Add(new SqlParameter("headers", headers));
        cmd.Parameters.Add(new SqlParameter("payload", bytes));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}