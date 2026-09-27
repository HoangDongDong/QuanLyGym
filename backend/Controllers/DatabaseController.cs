using Microsoft.AspNetCore.Mvc;
using FirebirdSql.Data.FirebirdClient;

namespace GymManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IConfiguration _config;

    public DatabaseController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("test")]
    public IActionResult TestConnection()
    {
        var connStr = _config.GetConnectionString("FirebirdConnection");
        try
        {
            using var conn = new FbConnection(connStr);
            conn.Open();
            return Ok(new { success = true, message = "Kết nối thành công tới Firebird!", serverVersion = conn.ServerVersion });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public IActionResult GetTables()
    {
        var connStr = _config.GetConnectionString("FirebirdConnection");
        var tables = new List<string>();
        try
        {
            using var conn = new FbConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 ORDER BY RDB$RELATION_NAME";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0).Trim());
            }
            return Ok(new { success = true, count = tables.Count, tables });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("query/{tableName}")]
    public IActionResult QueryTable(string tableName, [FromQuery] int limit = 10)
    {
        var connStr = _config.GetConnectionString("FirebirdConnection");
        var list = new List<Dictionary<string, object?>>();
        try
        {
            using var conn = new FbConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT FIRST {limit} * FROM {tableName.ToUpper()}";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                list.Add(row);
            }
            return Ok(new { success = true, count = list.Count, data = list });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("columns/{tableName}")]
    public IActionResult GetColumns(string tableName)
    {
        var connStr = _config.GetConnectionString("FirebirdConnection");
        var cols = new List<string>();
        try
        {
            using var conn = new FbConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tableName.ToUpper()} WHERE 1=0";
            using var reader = cmd.ExecuteReader();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                cols.Add(reader.GetName(i));
            }
            return Ok(new { success = true, columns = cols });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
