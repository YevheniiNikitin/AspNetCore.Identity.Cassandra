using System;
using Cassandra.Mapping.Attributes;
using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.Cassandra.Models;

public class PhoneInfo
{
    /// <summary>
    /// Gets or sets a telephone number for the user.
    /// </summary>
    [ProtectedPersonalData]
    public string? Number { get; internal set; }

    public DateTimeOffset? ConfirmationTime { get; internal set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their telephone address.
    /// </summary>
    /// <value>True if the telephone number has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool IsConfirmed => ConfirmationTime is not null;

    [Ignore]
    public bool AllPropertiesAreSetToDefaults =>
        Number == null &&
        ConfirmationTime == null;

    public static implicit operator PhoneInfo(string? input)
        => new() { Number = input };
}