using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BusinessService.Infrastructure.Context;

public class DapperContext
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    static DapperContext()
    {
        // Configure Dapper to map snake_case columns to PascalCase properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DapperContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("PostgresConnection")!;
    }

    public virtual IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}