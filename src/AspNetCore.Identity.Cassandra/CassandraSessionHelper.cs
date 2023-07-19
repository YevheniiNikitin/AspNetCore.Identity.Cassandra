namespace AspNetCore.Identity.Cassandra;

public static class CassandraSessionHelper
{
    public static string? UsersTableName { get; set; }
    public static string? RolesTableName { get; set; }
}