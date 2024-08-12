﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UseAwaitUsing

namespace Freakout.MsSql.Internals;

/// <summary>
/// This one should probably be changed into one that does not manage its own connection/transaction
/// </summary>
class MsSqlOutbox(MsSqlFreakoutConfiguration configuration, IFreakoutContextAccessor freakoutContextAccessor) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        
        var context = freakoutContextAccessor.GetContext<MsSqlFreakoutContext>();
        var transaction = context.Transaction;

        transaction.AddOutboxCommand(
            serializer: configuration.CommandSerializer,
            schemaName: configuration.SchemaName,
            tableName: configuration.TableName,
            command: command,
            headers: headers
        );
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = freakoutContextAccessor.GetContext<MsSqlFreakoutContext>();
        var transaction = context.Transaction;

        await transaction.AddOutboxCommandAsync(
            serializer: configuration.CommandSerializer,
            schemaName: configuration.SchemaName,
            tableName: configuration.TableName,
            command: command,
            headers: headers,
            cancellationToken: cancellationToken
        );
    }
}