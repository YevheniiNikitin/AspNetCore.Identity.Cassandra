using System;
using Cassandra.Mapping.Attributes;

namespace AspNetCore.Identity.Cassandra.Models;

public class CassandraIdentityRole
{
    /// <summary>
    /// Gets or sets the primary key for this role.
    /// </summary>
    [PartitionKey]
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets or sets the name for this role.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the normalized name for this role.
    /// </summary>
    public string? NormalizedName { get; set; }

    public CassandraIdentityRole()
    {

    }

    public CassandraIdentityRole(Guid id)
        : this()
    {
        Id = id;
    }
}