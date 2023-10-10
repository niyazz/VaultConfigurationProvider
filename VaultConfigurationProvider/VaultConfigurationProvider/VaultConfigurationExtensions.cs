using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.LDAP;

namespace VaultConfigurationProvider;

public static class VaultConfigurationExtensions
{
    /// <summary>
    /// Adds Vault as a configuration provider for local development.
    /// </summary>
    /// <remarks>Authentication using LDAP.</remarks>
    /// <param name="configuration">Instance of the configuration builder.</param>
    /// <param name="optionsAction">Options for connecting to the storage.</param>
    /// <param name="userSecretsId">The name of the userSecretsId to use. Defaults to localdevvault.</param>
    /// <param name="optional">A sign of mandatory. An exception will be thrown if the keys could not be loaded.</param>
    /// <returns><see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddLocalDevelopmentVault(this IConfigurationBuilder configuration,
        Action<VaultConfigurationOptions> optionsAction, string userSecretsId = "localdevvault", bool optional = true)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _ = optionsAction ?? throw new ArgumentNullException(nameof(optionsAction));

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            configuration.AddUserSecrets(userSecretsId);
            configuration.AddVault(optionsAction, optional);
        }

        return configuration;
    }

    /// <summary>
    /// Adds Vault as a configuration provider.
    /// </summary>
    /// <remarks>Authentication using LDAP.</remarks>
    /// <param name="configuration">Instance of the configuration builder.</param>
    /// <param name="optionsAction">Options for connecting to the storage.</param>
    /// <param name="optional">A sign of mandatory. An exception will be thrown if the keys could not be loaded.</param>
    /// <returns><see cref="IConfigurationBuilder"/>.</returns>
    private static IConfigurationBuilder AddVault(this IConfigurationBuilder configuration,
        Action<VaultConfigurationOptions> optionsAction, bool optional = true)
    {
        var vaultProviderConfiguration = configuration.Build().GetSection("VaultProvider");
        var options = new VaultConfigurationOptions();
        optionsAction.Invoke(options);
        try
        {
            var settings = new VaultClientSettings(options.BaseUrl.EnsureTrailingSlash(),
                new LDAPAuthMethodInfo(vaultProviderConfiguration["Username"], vaultProviderConfiguration["Password"]))
            {
                MyHttpClientProviderFunc = _ =>
                {
                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return new HttpClient(httpClientHandler);
                },
                ContinueAsyncTasksOnCapturedContext = false
            };
            var vaultClient = new VaultClient(settings);
            var vaultConfigurationSource = new VaultConfigurationSource(vaultClient, options, optional);
            configuration.Add(vaultConfigurationSource);
        }
        catch (Exception) when (optional) { }
        
        return configuration;
    }
}