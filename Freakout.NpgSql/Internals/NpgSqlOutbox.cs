using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace Freakout.NpgSql.Internals;

class NpgSqlOutbox(IFreakoutContextAccessor freakoutContextAccessor) : IOutbox
{
    public void AddOutboxCommand(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = freakoutContextAccessor.GetContext<NpgsqlFreakoutContext>();
        var transaction = context.Transaction;
        transaction.AddOutboxCommand(command, headers);
    }

    public async Task AddOutboxCommandAsync(object command, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = freakoutContextAccessor.GetContext<NpgsqlFreakoutContext>();
        var transaction = context.Transaction;
        await transaction.AddOutboxCommandAsync(command, headers, cancellationToken: cancellationToken);
    }
}