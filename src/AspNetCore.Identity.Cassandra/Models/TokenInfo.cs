using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.Cassandra.Models;

public class TokenInfo
{
    /// <summary>
    /// Gets or sets the LoginProvider this token is from.
    /// </summary>
    public string LoginProvider { get; internal set; }

    /// <summary>
    /// Gets or sets the name of the token.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    [ProtectedPersonalData]
    public string? Value { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TokenInfo()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public TokenInfo(string loginProvider, string name, string? value)
    {
        LoginProvider = loginProvider;
        Name = name;
        Value = value;
    }
}