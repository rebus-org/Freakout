using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Freakout.MsSql;

public static class SqlConnectionExtensions
{
    public static async Task AddOutboxCommandAsync(this SqlConnection connection, object command)
    {

    }
}