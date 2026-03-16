#r "nuget: Microsoft.Data.SqlClient, 5.2.0"
using System;
using System.Data;
using Microsoft.Data.SqlClient;

var cs = "Server=tcp:voltyks-server-2025.database.windows.net,1433;Initial Catalog=VoltyksDB;Persist Security Info=False;User ID=voltyksapp;Password=V0ltyks@ppSecure2025#!;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;";
var conn = new SqlConnection(cs);
conn.Open();
Console.WriteLine("Connected to database.");

void Check(string label, string sql)
{
    var cmd = new SqlCommand(sql, conn);
    var result = cmd.ExecuteScalar();
    Console.WriteLine($"{label}: {(result != null ? result.ToString() : "NOT FOUND")}");
}

Check("Connection test", "SELECT 1");
Check("Can read data", "SELECT TOP 1 Id FROM AspNetRoles");
Check("Session context", "EXEC sp_set_session_context @key = N'AllowAdminRoleInsert', @value = 1; SELECT CAST(SESSION_CONTEXT(N'AllowAdminRoleInsert') AS INT)");
Console.WriteLine("voltyksapp user connection: OK");
conn.Close();
