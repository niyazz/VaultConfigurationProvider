using Microsoft.Extensions.Configuration;
using VaultSharp;

namespace VaultConfigurationProvider;

/// <summary>
/// Vault configuration source.
/// </summary>
internal class VaultConfigurationSource : IConfigurationSource
{
    private readonly IVaultClient _client;
    private readonly VaultConfigurationOptions _options;
    private readonly bool _isOptional;

    public VaultConfigurationSource(IVaultClient client, VaultConfigurationOptions options, bool optional)
    {
        _client = client;
        _options = options;
        _isOptional = optional;
    }

    public IConfigurationProvider Build(IConfigurationBuilder configBuilder) =>
        new LdapVaultConfigurationProvider(_client, _options, _isOptional);
}