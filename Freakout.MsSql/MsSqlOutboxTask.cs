using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Freakout.MsSql;

class MsSqlOutboxTask(Func<CancellationToken, Task> executeAsync, SqlConnection sqlConnection, SqlTransaction sqlTransaction) : OutboxTask
{


    public override async Task ExecuteAsync()
    {

    }
}