using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BusinessService.Infrastructure.Context;

public class DapperContext
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
    }

    public virtual IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}