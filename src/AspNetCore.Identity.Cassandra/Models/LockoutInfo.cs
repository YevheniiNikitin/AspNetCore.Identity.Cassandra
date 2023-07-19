using System;
using Cassandra.Mapping.Attributes;

namespace AspNetCore.Identity.Cassandra.Models;

public class LockoutInfo
{
    /// <summary>
    /// Gets or sets the date and time, in UTC, when any user lockout ends.
    /// </summary>
    /// <remarks>
    /// A value in the past means the user is not locked out.
    /// </remarks>
    public DateTimeOffset? EndDate { get; internal set; }

    /// <summary>
    /// Gets or sets a flag indicating if the user could be locked out.
    /// </summary>
    /// <value>True if the user could be locked out, otherwise false.</value>
    public bool Enabled { get; internal set; }

    /// <summary>
    /// Gets or sets the number of failed login attempts for the current user.
    /// </summary>
    public int AccessFailedCount { get; internal set; }

    [Ignore]
    public bool AllPropertiesAreSetToDefaults =>
        EndDate == null &&
        Enabled == false &&
        AccessFailedCount == 0;
}