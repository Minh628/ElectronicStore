using System;
using Microsoft.Data.SqlClient;

class Program {
    static void Main() {
        var connStr = "Server=localhost;Database=ElectronicStoreDb;Trusted_Connection=True;TrustServerCertificate=True;";
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT UserName, PasswordHash, EmailConfirmed, IsActive, LockoutEnabled, LockoutEnd FROM AspNetUsers";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            Console.WriteLine($"{reader["UserName"]} | {reader["PasswordHash"]} | Active: {reader["IsActive"]}");
        }
    }
}
