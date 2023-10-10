namespace VaultConfigurationProvider;

/// <summary>
/// Configuration options used in <see cref="VaultConfigurationExtensions"/>.
/// </summary>
public class VaultConfigurationOptions
{
    /// <summary>
    /// Vault address.
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// Mount point of vault (default = secret).
    /// </summary>
    public string MountPoint { get; set; } = "secret";

    /// <summary>
    /// Base path to secrets (useful for microservices architecture).
    /// </summary>
    public string SecretsBasePath { get; set; }

    /// <summary>
    /// Base  path to shared secrets (useful for microservices architecture).
    /// </summary>
    public string SecretsCommonPath { get; set; }

    /// <summary>
    /// Provider should look up common path (default true).
    /// </summary>
    public bool UseSecretsCommonPath { get; set; } = true;

    /// <summary>
    /// Services secrets paths.
    /// </summary>
    public string[] SecretsSubPaths { get; set; }

    /// <summary>
    /// Use DefaultExceptKeys (default = true).
    /// </summary>
    public bool UseDefaultExceptKeys { get; set; } = true;

    /// <summary>
    /// Keys that will not be overwritten by default.
    /// </summary>
    public readonly string[] DefaultExceptKeys =
    {
        "Logging",
        "Kestrel",
    };

    /// <summary>
    /// Keys that will not be overwritten by custom rules.
    /// <remarks>Whole section name can be provided.</remarks>
    /// </summary>
    public string[] ExceptKeys { get; set; }

    /// <summary>
    /// Keys that should be overwritten even if their parents (or themselves in ExceptKeys).
    /// <remarks>Only complete path to key is allowed.</remarks>
    /// <example>App:IntegrationSystem:BaseUrl</example>
    /// </summary>
    public string[] RequiredKeys { get; set; }

    /// <summary>
    /// Prefix used to filter environment variables.
    /// </summary>
    public string EnvironmentVariablePrefix { get; set; } = "ENV_";

    /// <summary>
    /// Separator for hierarchical key in vault configuration.
    /// </summary>
    public string SourceSeparator { get; set; } = "__";

    /// <summary>
    /// Separator for hierarchical key in application configuration.
    /// </summary>
    public string DestinationSeparator { get; set; } = ":";
}
