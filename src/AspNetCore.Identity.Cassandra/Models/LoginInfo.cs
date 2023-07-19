using System;
using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.Cassandra.Models;

public class LoginInfo : IEquatable<LoginInfo>, IEquatable<UserLoginInfo>
{
    public string LoginProvider { get; internal set; }
    public string ProviderKey { get; internal set; }
    public string? ProviderDisplayName { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public LoginInfo()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {

    }

    public LoginInfo(string loginProvider, string providerKey, string? displayName)
    {
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        ProviderDisplayName = displayName;
    }

    public static implicit operator LoginInfo(UserLoginInfo input)
        => new(input.LoginProvider, input.ProviderKey, input.ProviderDisplayName);

    public bool Equals(LoginInfo? other) =>
        other is not null 
        && LoginProvider == other.LoginProvider 
        && ProviderKey == other.ProviderKey;

    public bool Equals(UserLoginInfo? other) =>
        other is not null 
        && LoginProvider == other.LoginProvider 
        && ProviderKey == other.ProviderKey;
}