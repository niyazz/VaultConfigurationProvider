using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;

namespace VaultConfigurationProvider;

/// <summary>
/// Vault configuration provider using LDAP.
/// </summary>
internal class LdapVaultConfigurationProvider : ConfigurationProvider
{
    private readonly IVaultClient _vaultClient;
    private readonly VaultConfigurationOptions _options;
    private readonly bool _isOptional;

    public LdapVaultConfigurationProvider(IVaultClient vaultClient, VaultConfigurationOptions options, bool optional)
    {
        _vaultClient = vaultClient;
        _options = options;
        _isOptional = optional;
    }

    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    private List<string> GetSecretsFullPaths()
    {
        if (_options.SecretsSubPaths is not {Length: > 0})
        {
            throw new ArgumentNullException(nameof(_options.SecretsSubPaths));
        }

        var secretsFullPaths = new List<string>();
        if (_options.UseSecretsCommonPath)
        {
            var section = _options.SecretsSubPaths[0].GetBeforeSlash();
            var secretCommonFullPath = _options.SecretsBasePath.EnsureTrailingSlash() + section.EnsureTrailingSlash() +
                                       _options.SecretsCommonPath.EnsureWithoutSlashes();
            secretsFullPaths.Add(secretCommonFullPath);
        }
        
        foreach (var subPath in _options.SecretsSubPaths)
        {
            var secretFullPath = _options.SecretsBasePath.EnsureTrailingSlash() + subPath;
            secretsFullPaths.Add(secretFullPath);
        }
        
        return secretsFullPaths;
    }
    
    private async Task LoadAsync()
    {
        try
        {
            var secretsFullPaths = GetSecretsFullPaths();
            var loadedSecrets = new Dictionary<string, object>();
            var tasks = new List<Task<Secret<SecretData>>>();
            foreach (var secretsFullPath in secretsFullPaths)
            {
                var task = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: secretsFullPath,
                    mountPoint: _options.MountPoint
                );
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var data in results.SelectMany(secret => secret.Data.Data))
            {
                if (loadedSecrets.ContainsKey(data.Key) == false)
                {
                    loadedSecrets.Add(data.Key, data.Value);
                }
                else
                {
                    loadedSecrets[data.Key] = data.Value;
                }
            }

            SetData(loadedSecrets);
        }
        catch (Exception) when (_isOptional)
        {
        }
        catch (Exception ex)
        {
            throw new VaultApiException(
                $"Error occurred while trying to get configuration from Vault. See inner exception: {ex.Message}", ex);
        }
    }

    private void SetData(IDictionary<string, object> loadedSecrets)
    {
        var customExceptKeys = _options.ExceptKeys ?? Array.Empty<string>();
        var exceptKeys = new HashSet<string>(_options.UseDefaultExceptKeys
            ? _options.DefaultExceptKeys.Concat(customExceptKeys)
            : customExceptKeys);
        var requiredKeys = new HashSet<string>(_options.RequiredKeys ?? Array.Empty<string>());

        foreach (var secretItem in loadedSecrets)
        {
            var prefixEndIndex = _options.EnvironmentVariablePrefix.IsNullOrWhiteSpace() == false
                ? _options.EnvironmentVariablePrefix.Length
                : 0;

            var sections = secretItem.Key[prefixEndIndex..].Split(_options.SourceSeparator);
            var transformedKey = string.Join(_options.DestinationSeparator, sections);

            if (requiredKeys.Contains(transformedKey))
            {
                Set(transformedKey, (string) secretItem.Value);
                continue;
            }

            if (exceptKeys.Contains(transformedKey) ||
                transformedKey.ContainsAny(_options.DestinationSeparator, exceptKeys.ToArray()))
            {
                continue;
            }

            Set(transformedKey, (string) secretItem.Value);
        }
    }
}