using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra.Mapping.Attributes;
using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.Cassandra.Models;

public class CassandraIdentityUser
{
    #region | Fields

    private readonly List<LoginInfo> _logins;
    private readonly List<TokenInfo> _tokens;
    private readonly List<string> _roles;

    #endregion

    #region | Properties

    /// <summary>
    /// Gets or sets the primary key for this user.
    /// </summary>
    [PartitionKey]
    public Guid Id { get; internal set; }

    /// <summary>
    /// Gets or sets the user name for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the normalized user name for this user.
    /// </summary>
    public string? NormalizedUserName { get; set; }

    /// <summary>
    /// Gets or sets the email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the normalized email address for this user.
    /// </summary>
    public string? NormalizedEmail { get; set; }

    /// <summary>
    /// Gets or sets the email confirmation time for this user.
    /// </summary>
    public DateTimeOffset? EmailConfirmationTime { get; set; }

    /// <summary>
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    public string? PasswordHash { get; internal set; }

    /// <summary>
    /// A random value that must change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public string? SecurityStamp { get; internal set; }

    [Frozen]
    public PhoneInfo? Phone { get; internal set; }

    /// <summary>
    /// Gets or sets a flag indicating if two factor authentication is enabled for this user.
    /// </summary>
    /// <value>True if 2fa is enabled, otherwise false.</value>
    [PersonalData]
    public bool TwoFactorEnabled { get; internal set; }

    [Frozen]
    public LockoutInfo? Lockout { get; internal set; }

    [Frozen]
    public IEnumerable<LoginInfo> Logins
    {
        get => _logins;
        internal set
        {
            if (value != null)
                _logins.AddRange(value);
        }
    }

    [Frozen]
    public IEnumerable<TokenInfo> Tokens
    {
        get => _tokens;
        internal set
        {
            if (value != null)
                _tokens.AddRange(value);
        }
    }

    [SecondaryIndex]
    public IEnumerable<string> Roles
    {
        get => _roles;
        internal set
        {
            if (value != null)
                _roles.AddRange(value);
        }
    }

    /// <summary>
    /// A flag that indicates if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    [PersonalData]
    [Ignore]
    public bool EmailConfirmed => EmailConfirmationTime.HasValue;

    #endregion

    #region | Constructors

    public CassandraIdentityUser()
    {
        _logins = new List<LoginInfo>();
        _tokens = new List<TokenInfo>();
        _roles = new List<string>();
    }

    public CassandraIdentityUser(Guid id)
        : this()
    {
        Id = id;
    }

    #endregion

    #region | Internal Methods

    internal void CleanUp()
    {
        if (Lockout is { AllPropertiesAreSetToDefaults: true })
            Lockout = null;

        if (Phone is { AllPropertiesAreSetToDefaults: true })
            Phone = null;
    }

    internal void AddLogin(LoginInfo? login)
    {
        ArgumentNullException.ThrowIfNull(login);

        if (_logins.Any(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
            throw new InvalidOperationException($"Login with LoginProvider: '{login.LoginProvider}' and ProviderKey: {login.ProviderKey} already exists.");

        _logins.Add(login);
    }

    internal void RemoveLogin(string loginProvider, string providerKey)
    {
        var loginToRemove = _logins.FirstOrDefault(l =>
            l.LoginProvider == loginProvider &&
            l.ProviderKey == providerKey
        );

        if (loginToRemove == null)
            return;

        _logins.Remove(loginToRemove);
    }

    internal void AddToken(TokenInfo? token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if(_tokens.Any(x => x.LoginProvider == token.LoginProvider && x.Name == token.Name))
            throw new InvalidOperationException($"Token with LoginProvider: '{token.LoginProvider}' and Name: {token.Name} already exists.");

        _tokens.Add(token);
    }

    internal void RemoveToken(string loginProvider, string name)
    {
        var tokenToRemove = _tokens.FirstOrDefault(l =>
            l.LoginProvider == loginProvider &&
            l.Name == name
        );

        if (tokenToRemove == null)
            return;

        _tokens.Remove(tokenToRemove);
    }

    internal void AddRole(string? role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (!_roles.Contains(role))
            _roles.Add(role);
    }

    internal void RemoveRole(string? role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_roles.Contains(role))
            _roles.Remove(role);
    }

    #endregion

    /// <summary>
    /// Returns the username for this user.
    /// </summary>
    public override string ToString()
        => UserName ?? string.Empty;
}