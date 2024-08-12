using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace Freakout.NpgSql.Internals;

class NpgsqlOutbox(NpgsqlFreakoutConfiguration configuration, IFreakoutContextAccessor freakoutContextAccessor) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = freakoutContextAccessor.GetContext<NpgsqlFreakoutContext>();
        var transaction = context.Transaction;

        transaction.AddOutboxCommand(
            schemaName: configuration.SchemaName,
            tableName: configuration.TableName,
            serializer: configuration.CommandSerializer,
            command: command,
            headers: headers
        );
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = freakoutContextAccessor.GetContext<NpgsqlFreakoutContext>();
        var transaction = context.Transaction;

        await transaction.AddOutboxCommandAsync(
            schemaName: configuration.SchemaName,
            tableName: configuration.TableName,
            serializer: configuration.CommandSerializer,
            command: command,
            headers: headers,
            cancellationToken: cancellationToken
        );
    }
}